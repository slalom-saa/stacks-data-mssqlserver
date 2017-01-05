using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Slalom.Stacks.Messaging.Logging;
using Slalom.Stacks.Validation;
using System.Linq;
using Newtonsoft.Json;

namespace Slalom.Stacks.Logging.SqlServer
{
    /// <summary>
    /// A SQL Server <see cref="ILogStore"/> implementation.
    /// </summary>
    /// <seealso cref="Slalom.Stacks.Messaging.Logging.ILogStore" />
    public class LogStore : PeriodicBatcher<LogEntry>, ILogStore
    {
        private readonly SqlConnectionManager _connection;
        private readonly DataTable _eventsTable;
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

            _eventsTable = this.CreateTable();
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

        public DataTable CreateTable()
        {
            var table = new DataTable(_options.LogTableName);

            table.Columns.Add(new DataColumn
            {
                DataType = typeof(int),
                ColumnName = "Id",
                AutoIncrement = true,
                AllowDBNull = false,
            });
            table.PrimaryKey = new[] { table.Columns[0] };
            table.Columns.Add(new DataColumn("ApplicationName")
            {
                DataType = typeof(string)
            });
            table.Columns.Add(new DataColumn("CommandId")
            {
                DataType = typeof(string)
            });
            table.Columns.Add(new DataColumn("CommandName")
            {
                DataType = typeof(string)
            });
            table.Columns.Add(new DataColumn("Completed")
            {
                DataType = typeof(DateTimeOffset)
            });
            table.Columns.Add(new DataColumn("CorrelationId")
            {
                DataType = typeof(string)
            });
            table.Columns.Add(new DataColumn("Elapsed")
            {
                DataType = typeof(TimeSpan)
            });
            table.Columns.Add(new DataColumn("Environment")
            {
                DataType = typeof(string)
            });
            table.Columns.Add(new DataColumn("Exception")
            {
                DataType = typeof(string)
            });
            table.Columns.Add(new DataColumn("IsSuccessful")
            {
                DataType = typeof(bool)
            });
            table.Columns.Add(new DataColumn("MachineName")
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
            table.Columns.Add(new DataColumn("SessionId")
            {
                DataType = typeof(string)
            });
            table.Columns.Add(new DataColumn("Started")
            {
                DataType = typeof(DateTimeOffset)
            });
            table.Columns.Add(new DataColumn("ThreadId")
            {
                DataType = typeof(string)
            });
            table.Columns.Add(new DataColumn("SourceAddress")
            {
                DataType = typeof(string)
            });
            table.Columns.Add(new DataColumn("UserName")
            {
                DataType = typeof(string)
            });
            table.Columns.Add(new DataColumn("ValidationErrors")
            {
                DataType = typeof(string)
            });
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
                    item.SourceAddress,
                    item.UserName,
                    item.ValidationErrors.Any() ? JsonConvert.SerializeObject(item.ValidationErrors) : null);
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