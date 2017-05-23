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
using Slalom.Stacks.Configuration;
using Slalom.Stacks.Logging.SqlServer.Batching;
using Slalom.Stacks.Logging.SqlServer.Locations;
using Slalom.Stacks.Services.Logging;
using Slalom.Stacks.Validation;

namespace Slalom.Stacks.Logging.SqlServer.Core
{
    /// <summary>
    /// A SQL Server <see cref="IResponseLog"/> implementation.
    /// </summary>
    /// <seealso cref="IResponseLog" />
    public class ResponseLog : PeriodicBatcher<ResponseEntry>, IResponseLog
    {
        private readonly SqlServerLoggingOptions _options;
        private readonly ILocationStore _locations;
        private readonly Application _environment;
        private readonly DataTable _eventsTable;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResponseLog" /> class.
        /// </summary>
        /// <param name="options">The configured <see cref="SqlServerLoggingOptions" />.</param>
        /// <param name="locations">The configured <see cref="LocationStore" />.</param>
        /// <param name="environment">The environment context.</param>
        public ResponseLog(SqlServerLoggingOptions options, ILocationStore locations, Application environment) : base(options.BatchSize, options.Period)
        {
            Argument.NotNull(options, nameof(options));
            Argument.NotNull(locations, nameof(locations));
            Argument.NotNull(environment, nameof(environment));

            _options = options;
            _locations = locations;
            _environment = environment;

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
            table.PrimaryKey = new[] {table.Columns[0]};
            table.Columns.Add(new DataColumn("EntryId")
            {
                DataType = typeof(string)
            });
            table.Columns.Add(new DataColumn("TimeStamp")
            {
                DataType = typeof(DateTimeOffset)
            });
            table.Columns.Add(new DataColumn("Endpoint")
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
            table.Columns.Add(new DataColumn("RequestId")
            {
                DataType = typeof(string)
            });
            table.Columns.Add(new DataColumn("Path")
            {
                DataType = typeof(string)
            });
            table.Columns.Add(new DataColumn("Started")
            {
                DataType = typeof(DateTimeOffset)
            });
            table.Columns.Add(new DataColumn("Version")
            {
                DataType = typeof(string)
            });
            table.Columns.Add(new DataColumn("Build")
            {
                DataType = typeof(string)
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
                    item.Id,
                    item.TimeStamp,
                    item.EndPoint,
                    item.ApplicationName,
                    item.Completed,
                    item.CorrelationId,
                    item.Elapsed,
                    item.EnvironmentName,
                    item.Exception,
                    item.IsSuccessful,
                    item.MachineName,
                    item.RequestId,
                    item.Path,
                    item.Started,
                    item.Version,
                    item.Build,
                    item.ValidationErrors.Any() ? String.Join(";#;", item.ValidationErrors.Select(e => e.Type + ": " + e.Message)) : null);
            }
            _eventsTable.AcceptChanges();
        }

        protected override async Task EmitBatchAsync(IEnumerable<ResponseEntry> events)
        {
            var list = events as IList<ResponseEntry> ?? events.ToList();

            this.Fill(list);

            using (var connection = new SqlConnection(_options.ConnectionString))
            {
                connection.Open();

                using (var copy = new SqlBulkCopy(connection))
                {
                    copy.DestinationTableName = string.Format(_options.ResponsesTableName);
                    foreach (var column in _eventsTable.Columns)
                    {
                        var columnName = ((DataColumn) column).ColumnName;
                        var mapping = new SqlBulkCopyColumnMapping(columnName, columnName);
                        copy.ColumnMappings.Add(mapping);
                    }

                    await copy.WriteToServerAsync(_eventsTable).ConfigureAwait(false);
                }
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

        public IEnumerable<ValidationError> GetValidationErrors(string value)
        {
            if (String.IsNullOrWhiteSpace(value))
            {
                yield break;
            }
            var items = value.Split(new[] {"#;#"}, StringSplitOptions.RemoveEmptyEntries);
            foreach (var item in items)
            {
                var type = (ValidationType) Enum.Parse(typeof(ValidationType), item.Substring(0, item.IndexOf(": ")));
                var message = item.Substring(item.IndexOf(": ") + 3);

                yield return new ValidationError(message, type);
            }
        }

        public async Task<IEnumerable<ResponseEntry>> GetEntries(DateTimeOffset? start, DateTimeOffset? end)
        {
            var builder = new StringBuilder($"SELECT TOP {_options.SelectLimit} * FROM {_options.ResponsesTableName} WHERE Not Id IS NULL");
            if (start.HasValue)
            {
                builder.Append(" AND TimeStamp >= \'" + start + "\'");
            }
            if (end.HasValue)
            {
                builder.Append(" AND TimeStamp <= \'" + end + "\'");
            }
            if (String.IsNullOrWhiteSpace(_environment.Title))
            {
                builder.Append(" AND ApplicationName is NULL");
            }
            else
            {
                builder.Append(" AND ApplicationName = \'" + _environment.Title + "\'");
            }
            if (String.IsNullOrWhiteSpace(_environment.Environment))
            {
                builder.Append(" AND Environment is NULL");
            }
            else
            {
                builder.Append(" AND Environment = \'" + _environment.Environment + "\'");
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
                                .Select(e => new ResponseEntry
                                {
                                    Id = e["EntryId"].GetValue<string>(),
                                    CorrelationId = e["CorrelationId"].GetValue<string>(),
                                    ApplicationName = e["ApplicationName"].GetValue<string>(),
                                    Completed = e["Completed"].GetValue<DateTimeOffset?>(),
                                    Elapsed = e["Elapsed"].GetValue<TimeSpan>(),
                                    EnvironmentName = e["Environment"].GetValue<string>(),
                                    Exception = e["Exception"].GetValue<string>(),
                                    IsSuccessful = e["IsSuccessful"].GetValue<bool>(),
                                    MachineName = e["MachineName"].GetValue<string>(),
                                    Path = e["Path"].GetValue<string>(),
                                    RequestId = e["RequestId"].GetValue<string>(),
                                    EndPoint = e["Endpoint"].GetValue<string>(),
                                    Started = e["Started"].GetValue<DateTimeOffset>(),
                                    Version = e["Version"].GetValue<string>(),
                                    Build = e["Build"].GetValue<string>(),
                                    TimeStamp = e["TimeStamp"].GetValue<DateTimeOffset>(),
                                    ValidationErrors = this.GetValidationErrors(e["ValidationErrors"].GetValue<string>()),
                                });
                        }
                    }
                }
            }
        }
    }
}