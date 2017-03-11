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
    /// A SQL Server <see cref="IResponseLog"/> implementation.
    /// </summary>
    /// <seealso cref="IResponseLog" />
    public class ResponseLog : PeriodicBatcher<ResponseEntry>, IResponseLog
    {
        private readonly SqlServerLoggingOptions _options;
        private readonly SqlConnectionManager _connection;
        private readonly LocationStore _locations;
        private readonly DataTable _eventsTable;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResponseLog" /> class.
        /// </summary>
        /// <param name="options">The configured <see cref="SqlServerLoggingOptions" />.</param>
        /// <param name="connection">The configured <see cref="SqlConnectionManager" />.</param>
        /// <param name="locations">The configured <see cref="LocationStore" />.</param>
        public ResponseLog(SqlServerLoggingOptions options, SqlConnectionManager connection, LocationStore locations) : base(options.BatchSize, options.Period)
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
            var table = new DataTable(_options.ResponsesTableName);
            table.Columns.Add(new DataColumn
            {
                DataType = typeof(int),
                ColumnName = "Id",
                AutoIncrement = true,
                AllowDBNull = false
            });
            table.PrimaryKey = new[] { table.Columns[0] };
            table.Columns.Add(new DataColumn("Service")
            {
                DataType = typeof(string)
            });
            table.Columns.Add(new DataColumn("ApplicationName")
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
            table.Columns.Add(new DataColumn("MessageId")
            {
                DataType = typeof(string)
            });
            table.Columns.Add(new DataColumn("Started")
            {
                DataType = typeof(DateTimeOffset)
            });
            table.Columns.Add(new DataColumn("ThreadId")
            {
                DataType = typeof(int)
            });
            table.Columns.Add(new DataColumn("ValidationErrors")
            {
                DataType = typeof(string)
            });
            return table;
        }

        public void Fill(IEnumerable<ResponseEntry> entries)
        {
            foreach (var item in entries)
            {
                _eventsTable.Rows.Add(null,
                   item.Service,
                   item.ApplicationName,
                   item.Completed,
                   item.CorrelationId,
                   item.Elapsed,
                   item.EnvironmentName,
                   item.Exception,
                   item.IsSuccessful,
                   item.MachineName,
                   item.MessageId,
                   item.Started,
                   item.ThreadId,
                   item.ValidationErrors.Any() ? String.Join("  ", item.ValidationErrors.Select(e => e.Type + ": " + e.Message)) : null);
            }
            _eventsTable.AcceptChanges();
        }

        protected override async Task EmitBatchAsync(IEnumerable<ResponseEntry> events)
        {
            var list = events as IList<ResponseEntry> ?? events.ToList();

            this.Fill(list);

            using (var copy = new SqlBulkCopy(_connection.Connection))
            {
                copy.DestinationTableName = string.Format(_options.ResponsesTableName);
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
        public Task Append(ResponseEntry audit)
        {
            Argument.NotNull(audit, nameof(audit));

            this.Emit(audit);

            return Task.FromResult(0);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                _eventsTable.Dispose();
            }
        }

        public Task<IEnumerable<ResponseEntry>> GetEntries(DateTimeOffset? start, DateTimeOffset? end)
        {
            throw new NotImplementedException();
        }
    }
}