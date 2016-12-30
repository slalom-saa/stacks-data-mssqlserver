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
    /// A SQL Server <see cref="ILogStore"/> implementation.
    /// </summary>
    /// <seealso cref="Slalom.Stacks.Messaging.Logging.ILogStore" />
    public class LogStore : PeriodicBatcher<LogEntry>, ILogStore
    {
        private readonly SqlConnectionManager _connection;
        private readonly DataTable _eventsTable = CreateTable();
        private readonly SqlServerLoggingOptions _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="LogStore" /> class.
        /// </summary>
        /// <param name="options">The configured options.</param>
        /// <param name="connection">The configured <see cref="SqlConnectionManager" />.</param>
        public LogStore(SqlServerLoggingOptions options, SqlConnectionManager connection)
            : base(options.BatchSize, options.Period)
        {
            Argument.NotNull(options, nameof(options));
            Argument.NotNull(connection, nameof(connection));

            _options = options;
            _connection = connection;
        }

        /// <summary>
        /// Appends an audit with the specified execution elements.
        /// </summary>
        /// <param name="entry">The log entry to append.</param>
        /// <returns>A task for asynchronous programming.</returns>
        public async Task AppendAsync(LogEntry entry)
        {
            Argument.NotNull(entry, nameof(entry));

            this.Emit(entry);
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
            table.Columns.Add("CommandId");
            table.Columns.Add("CommandName");
            table.Columns.Add("Completed");
            table.Columns.Add("CorrelationId");
            table.Columns.Add("Elapsed");
            table.Columns.Add("Environment");
            table.Columns.Add("Exception");
            table.Columns.Add("IsSuccessful");
            table.Columns.Add("MachineName");
            table.Columns.Add("Path");
            table.Columns.Add("Payload");
            table.Columns.Add("SessionId");
            table.Columns.Add("Started");
            table.Columns.Add("ThreadId");
            table.Columns.Add("UserHostAddress");
            table.Columns.Add("UserName");
            table.Columns.Add("ValidationErrors");
            return table;
        }

        public void Fill(IEnumerable<LogEntry> entries)
        {
            foreach (var item in entries)
            {
                _eventsTable.Rows.Add(null,
                    item.ApplicationName,
                    item.CommandId,
                    item.CommandName,
                    item.Completed,
                    item.CorrelationId,
                    item.Elapsed,
                    item.Environment,
                    item.RaisedException?.ToString(),
                    item.IsSuccessful,
                    item.MachineName,
                    item.Path,
                    item.Payload,
                    item.SessionId,
                    item.Started,
                    item.ThreadId,
                    item.UserHostAddress,
                    item.UserName,
                    item.ValidationErrors);
            }
            _eventsTable.AcceptChanges();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                _eventsTable.Dispose();
            }
        }

        protected override async Task EmitBatchAsync(IEnumerable<LogEntry> events)
        {
            this.Fill(events);

            using (var copy = new SqlBulkCopy(_connection.Connection))
            {
                copy.DestinationTableName = string.Format(_options.LogTableName);
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
    }
}