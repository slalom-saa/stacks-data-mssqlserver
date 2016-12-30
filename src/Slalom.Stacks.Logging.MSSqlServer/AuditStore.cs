using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Slalom.Stacks.Messaging.Logging;
using Slalom.Stacks.Validation;

namespace Slalom.Stacks.Logging.MSSqlServer
{
    /// <summary>
    /// A SQL Server <see cref="IAuditStore"/> implementation.
    /// </summary>
    /// <seealso cref="Slalom.Stacks.Messaging.Logging.IAuditStore" />
    public class AuditStore : PeriodicBatcher<AuditEntry>, IAuditStore
    {
        private readonly SqlServerLoggingOptions _options;
        private readonly SqlConnectionManager _connection;
        private readonly DataTable _eventsTable = CreateTable();

        /// <summary>
        /// Initializes a new instance of the <see cref="AuditStore" /> class.
        /// </summary>
        /// <param name="options">The configured <see cref="SqlServerLoggingOptions"/>.</param>
        /// <param name="connection">The configured <see cref="SqlConnectionManager" />.</param>
        public AuditStore(SqlServerLoggingOptions options, SqlConnectionManager connection) : base(options.BatchSize, options.Period)
        {
            Argument.NotNull(options, nameof(options));
            Argument.NotNull(connection, nameof(connection));

            _options = options;
            _connection = connection;
        }

        public static DataTable CreateTable()
        {
            var table = new DataTable();
            table.Columns.Add(new DataColumn
            {
                DataType = typeof(int),
                ColumnName = "Id",
                AutoIncrement = true
            });
            table.Columns.Add("ApplicationName");
            table.Columns.Add("CorrelationId");
            table.Columns.Add("Environment");
            table.Columns.Add("EventId");
            table.Columns.Add("EventName");
            table.Columns.Add("MachineName");
            table.Columns.Add("Path");
            table.Columns.Add("Payload");
            table.Columns.Add("SessionId");
            table.Columns.Add("ThreadId");
            table.Columns.Add("TimeStamp");
            table.Columns.Add("UserHostAddress");
            table.Columns.Add("UserName");
            return table;
        }

        public void Fill(IEnumerable<AuditEntry> entries)
        {
            foreach (var item in entries)
            {
                _eventsTable.Rows.Add(null,
                    item.ApplicationName,
                    item.CorrelationId,
                    item.Environment,
                    item.EventId,
                    item.EventName,
                    item.MachineName,
                    item.Path,
                    item.Payload,
                    item.SessionId,
                    item.ThreadId,
                    item.TimeStamp,
                    item.UserHostAddress,
                    item.UserName);
            }
            _eventsTable.AcceptChanges();
        }

        protected override async Task EmitBatchAsync(IEnumerable<AuditEntry> events)
        {
            this.Fill(events);

            using (var copy = new SqlBulkCopy(_connection.Connection))
            {
                copy.DestinationTableName = string.Format(_options.AuditTableName);
                foreach (var column in _eventsTable.Columns)
                {
                    var columnName = ((DataColumn)column).ColumnName;
                    var mapping = new SqlBulkCopyColumnMapping(columnName, columnName);
                    copy.ColumnMappings.Add(mapping);
                }

                await copy.WriteToServerAsync(_eventsTable).ConfigureAwait(false);
            }
            _eventsTable.Clear();
        }

        /// <summary>
        /// Appends an audit with the specified execution elements.
        /// </summary>
        /// <param name="audit">The audit entry to append.</param>
        /// <returns>A task for asynchronous programming.</returns>
        public async Task AppendAsync(AuditEntry audit)
        {
            Argument.NotNull(audit, nameof(audit));

            this.Emit(audit);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                _eventsTable.Dispose();
            }
        }

    }
}