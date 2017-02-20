using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Slalom.Stacks.Messaging.Logging;
using Slalom.Stacks.Validation;

namespace Slalom.Stacks.Logging.SqlServer
{
    /// <summary>
    /// A SQL Server <see cref="IEventStore"/> implementation.
    /// </summary>
    /// <seealso cref="IEventStore" />
    public class AuditStore : PeriodicBatcher<EventEntry>, IEventStore
    {
        private readonly SqlServerLoggingOptions _options;
        private readonly SqlConnectionManager _connection;
        private readonly LocationStore _locations;
        private readonly DataTable _eventsTable;

        /// <summary>
        /// Initializes a new instance of the <see cref="AuditStore" /> class.
        /// </summary>
        /// <param name="options">The configured <see cref="SqlServerLoggingOptions" />.</param>
        /// <param name="connection">The configured <see cref="SqlConnectionManager" />.</param>
        /// <param name="locations">The configured <see cref="LocationStore" />.</param>
        public AuditStore(SqlServerLoggingOptions options, SqlConnectionManager connection, LocationStore locations) : base(options.BatchSize, options.Period)
        {
            Argument.NotNull(options, nameof(options));
            Argument.NotNull(connection, nameof(connection));
            Argument.NotNull(locations, nameof(locations));

            _options = options;
            _connection = connection;
            _locations = locations;

            _eventsTable = this.CreateTable();
        }

        public DataTable CreateTable()
        {
            var table = new DataTable(_options.EventsTableName);
            table.Columns.Add(new DataColumn
            {
                DataType = typeof(int),
                ColumnName = "Id",
                AutoIncrement = true,
                AllowDBNull = false
            });
            table.PrimaryKey = new[] { table.Columns[0] };
            table.Columns.Add(new DataColumn("EventId")
            {
                DataType = typeof(string)
            });
            table.Columns.Add(new DataColumn("EventName")
            {
                DataType = typeof(string)
            });
            table.Columns.Add(new DataColumn("EventTypeId")
            {
                DataType = typeof(int)
            });
            table.Columns.Add(new DataColumn("CorrelationId")
            {
                DataType = typeof(string)
            });
            table.Columns.Add(new DataColumn("ApplicationName")
            {
                DataType = typeof(string)
            });
            table.Columns.Add(new DataColumn("Environment")
            {
                DataType = typeof(string)
            });
            table.Columns.Add(new DataColumn("TimeStamp")
            {
                DataType = typeof(DateTimeOffset)
            });
            table.Columns.Add(new DataColumn("MachineName")
            {
                DataType = typeof(string)
            });
            table.Columns.Add(new DataColumn("ThreadId")
            {
                DataType = typeof(string)
            });
            table.Columns.Add(new DataColumn("Path")
            {
                DataType = typeof(string)
            });
            table.Columns.Add(new DataColumn("Payload")
            {
                DataType = typeof(string)
            });
            table.Columns.Add(new DataColumn("SourceAddress")
            {
                DataType = typeof(string)
            });
            table.Columns.Add(new DataColumn("SessionId")
            {
                DataType = typeof(string)
            });
            table.Columns.Add(new DataColumn("UserName")
            {
                DataType = typeof(string)
            });
            return table;
        }

        public void Fill(IEnumerable<EventEntry> entries)
        {
            foreach (var item in entries)
            {
                _eventsTable.Rows.Add(null,
                    item.EventId,
                    item.EventName,
                    item.EventTypeId,
                    item.CorrelationId,
                    item.ApplicationName,
                    item.Environment,
                    item.TimeStamp,
                    item.MachineName,
                    item.ThreadId,
                    item.Path,
                    item.Payload,
                    item.SourceAddress,
                    item.SessionId,
                    item.UserName);
            }
            _eventsTable.AcceptChanges();
        }

        protected override async Task EmitBatchAsync(IEnumerable<EventEntry> events)
        {
            var list = events as IList<EventEntry> ?? events.ToList();

            this.Fill(list);

            using (var copy = new SqlBulkCopy(_connection.Connection))
            {
                copy.DestinationTableName = string.Format(_options.EventsTableName);
                foreach (var column in _eventsTable.Columns)
                {
                    var columnName = ((DataColumn)column).ColumnName;
                    var mapping = new SqlBulkCopyColumnMapping(columnName, columnName);
                    copy.ColumnMappings.Add(mapping);
                }

                await copy.WriteToServerAsync(_eventsTable).ConfigureAwait(false);
            }
            _eventsTable.Clear();

            await _locations.UpdateAsync(list.Select(e => e.SourceAddress).Distinct().ToArray()).ConfigureAwait(false);
        }

        /// <summary>
        /// Appends an audit with the specified execution elements.
        /// </summary>
        /// <param name="audit">The audit entry to append.</param>
        /// <returns>A task for asynchronous programming.</returns>
        public async Task AppendAsync(EventEntry audit)
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