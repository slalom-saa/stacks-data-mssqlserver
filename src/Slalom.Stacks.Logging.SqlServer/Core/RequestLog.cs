/* 
 * Copyright (c) Stacks Contributors
 * 
 * This file is subject to the terms and conditions defined in
 * the LICENSE file, which is part of this source code package.
 */

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Slalom.Stacks.Logging.SqlServer.Batching;
using Slalom.Stacks.Logging.SqlServer.Locations;
using Slalom.Stacks.Runtime;
using Slalom.Stacks.Services.Logging;
using Slalom.Stacks.Services.Messaging;
using Slalom.Stacks.Validation;

namespace Slalom.Stacks.Logging.SqlServer.Core
{
    /// <summary>
    /// A SQL Server <see cref="IRequestStore"/> implementation.
    /// </summary>
    public class RequestLog : PeriodicBatcher<RequestEntry>, IRequestLog
    {
        private readonly ILocationStore _locations;
        private readonly IEnvironmentContext _environment;
        private readonly DataTable _eventsTable;
        private readonly SqlServerLoggingOptions _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="RequestLog" /> class.
        /// </summary>
        /// <param name="options">The configured options.</param>
        /// <param name="locations">The configured <see cref="ILocationStore" />.</param>
        /// <param name="environment">The environment context.</param>
        public RequestLog(SqlServerLoggingOptions options, ILocationStore locations, IEnvironmentContext environment)
            : base(options.BatchSize, options.Period)
        {
            Argument.NotNull(options, nameof(options));
            Argument.NotNull(locations, nameof(locations));
            Argument.NotNull(environment, nameof(environment));

            _options = options;
            _locations = locations;
            _environment = environment;

            _eventsTable = this.CreateTable();
        }

        /// <summary>
        /// Appends an audit with the specified execution elements.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>A task for asynchronous programming.</returns>
        public Task Append(Request request)
        {
            Argument.NotNull(request, nameof(request));

            this.Emit(new RequestEntry(request, _environment.Resolve()));

            return Task.FromResult(0);
        }

        public DataTable CreateTable()
        {
            var table = new DataTable(_options.RequestsTableName);

            table.Columns.Add(new DataColumn
            {
                DataType = typeof(int),
                ColumnName = "Id",
                AllowDBNull = false,
                AutoIncrement = true
            });
            table.PrimaryKey = new[] { table.Columns[0] };
            table.Columns.Add(new DataColumn("EntryId")
            {
                DataType = typeof(string)
            });
            table.Columns.Add(new DataColumn("CorrelationId")
            {
                DataType = typeof(string)
            });
            table.Columns.Add(new DataColumn("Body")
            {
                DataType = typeof(string)
            });
            table.Columns.Add(new DataColumn("RequestId")
            {
                DataType = typeof(string)
            });
            table.Columns.Add(new DataColumn("RequestType")
            {
                DataType = typeof(string)
            });
            table.Columns.Add(new DataColumn("Parent")
            {
                DataType = typeof(string)
            });
            table.Columns.Add(new DataColumn("Path")
            {
                DataType = typeof(string)
            });
            table.Columns.Add(new DataColumn("SessionId")
            {
                DataType = typeof(string)
            });
            table.Columns.Add(new DataColumn("SourceAddress")
            {
                DataType = typeof(string)
            });
            table.Columns.Add(new DataColumn("TimeStamp")
            {
                DataType = typeof(DateTimeOffset)
            });
            table.Columns.Add(new DataColumn("UserName")
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
            table.Columns.Add(new DataColumn("MachineName")
            {
                DataType = typeof(string)
            });
            table.Columns.Add(new DataColumn("ThreadId")
            {
                DataType = typeof(int)
            });
            return table;
        }

        public void Fill(IEnumerable<RequestEntry> entries)
        {
            foreach (var item in entries)
            {
                _eventsTable.Rows.Add(null,
                    item.Id,
                    item.CorrelationId,
                    item.Body,
                    item.RequestId,
                    item.RequestType,
                    item.Parent,
                    item.Path,
                    item.SessionId,
                    item.SourceAddress,
                    item.TimeStamp,
                    item.UserName,
                    item.ApplicationName,
                    item.EnvironmentName,
                    item.MachineName,
                    item.ThreadId);
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

        protected override async Task EmitBatchAsync(IEnumerable<RequestEntry> events)
        {
            var list = events as IList<RequestEntry> ?? events.ToList();
            this.Fill(list);

            using (var connection = new SqlConnection(_options.ConnectionString))
            {
                connection.Open();
                using (var copy = new SqlBulkCopy(connection))
                {
                    copy.DestinationTableName = string.Format(_options.RequestsTableName);
                    foreach (var column in _eventsTable.Columns)
                    {
                        var columnName = ((DataColumn)column).ColumnName;
                        var mapping = new SqlBulkCopyColumnMapping(columnName, columnName);
                        copy.ColumnMappings.Add(mapping);
                    }

                    await copy.WriteToServerAsync(_eventsTable).ConfigureAwait(false);
                }
            }
            _eventsTable.Clear();

            await _locations.Append(list.Select(e => e.SourceAddress).Distinct().ToArray()).ConfigureAwait(false);
        }

        public async Task<IEnumerable<RequestEntry>> GetEntries(DateTimeOffset? start, DateTimeOffset? end)
        {
            var builder = new StringBuilder($"SELECT TOP {_options.SelectLimit} * FROM {_options.RequestsTableName} WHERE Not Id IS NULL");
            if (start.HasValue)
            {
                builder.Append(" AND TimeStamp >= \'" + start + "\'");
            }
            if (end.HasValue)
            {
                builder.Append(" AND TimeStamp <= \'" + end + "\'");
            }
            var environment = _environment.Resolve();
            if (String.IsNullOrWhiteSpace(environment.ApplicationName))
            {
                builder.Append(" AND ApplicationName is NULL");
            }
            else
            {
                builder.Append(" AND ApplicationName = \'" + environment.ApplicationName + "\'");
            }
            if (String.IsNullOrWhiteSpace(environment.EnvironmentName))
            {
                builder.Append(" AND Environment is NULL");
            }
            else
            {
                builder.Append(" AND Environment = \'" + environment.EnvironmentName + "\'");
            }
            using (var connection = new SqlConnection(_options.ConnectionString))
            {
                connection.Open();
                using (var command = new SqlCommand(builder.ToString(), connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        using (var table = this.CreateTable())
                        {
                            table.Load(reader);
                            return table.Rows.OfType<DataRow>()
                                .Select(e => new RequestEntry
                                {
                                    CorrelationId = e["CorrelationId"].GetValue<string>(),
                                    Id = e["EntryId"].GetValue<string>(),
                                    Body = e["Body"].GetValue<string>(),
                                    RequestType = e["RequestType"].GetValue<string>(),
                                    Parent = e["Parent"].GetValue<string>(),
                                    Path = e["Path"].GetValue<string>(),
                                    SessionId = e["SessionId"].GetValue<string>(),
                                    SourceAddress = e["SourceAddress"].GetValue<string>(),
                                    TimeStamp = e["TimeStamp"].GetValue<DateTimeOffset?>(),
                                    UserName = e["UserName"].GetValue<string>(),
                                    RequestId = e["RequestId"].GetValue<string>(),
                                    ApplicationName = e["ApplicationName"].GetValue<string>(),
                                    EnvironmentName = e["Environment"].GetValue<string>(),
                                    MachineName = e["MachineName"].GetValue<string>(),
                                    ThreadId = e["ThreadId"].GetValue<int>(),
                                });
                        }
                    }
                }
            }
        }
    }

    internal static class Ext
    {
        public static T GetValue<T>(this object instance)
        {
            if (instance == null || instance == DBNull.Value)
            {
                return default(T);
            }
            return (T)instance;
        }
    }
}