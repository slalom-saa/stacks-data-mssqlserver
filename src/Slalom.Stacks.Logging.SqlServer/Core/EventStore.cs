/*
 * Copyright 2017 Stacks Contributors
 * 
 * This file is subject to the terms and conditions defined in
 * file 'LICENSE.txt', which is part of this source code package.
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
using Slalom.Stacks.Services.Messaging;
using Slalom.Stacks.Validation;

namespace Slalom.Stacks.Logging.SqlServer.Core
{
    public class EventStore : PeriodicBatcher<EventEntry>, IEventStore
    {
        private SqlServerLoggingOptions _options;
        private ILocationStore _locations;
        private Application _environment;
        private DataTable _eventsTable;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventStore" /> class.
        /// </summary>
        /// <param name="options">The configured <see cref="SqlServerLoggingOptions" />.</param>
        /// <param name="locations">The configured <see cref="ILocationStore" />.</param>
        /// <param name="environment">The environment context.</param>
        public EventStore(SqlServerLoggingOptions options, ILocationStore locations, Application environment) : base(options.BatchSize, options.Period)
        {
            Argument.NotNull(options, nameof(options));
            Argument.NotNull(locations, nameof(locations));
            Argument.NotNull(environment, nameof(environment));

            _options = options;
            _locations = locations;
            _environment = environment;

            _eventsTable = this.CreateTable();
        }

        public void Fill(IEnumerable<EventEntry> entries)
        {
            foreach (var item in entries)
            {
                _eventsTable.Rows.Add(null,
                    item.Id,
                    item.ApplicationName,
                    item.Body,
                    item.EnvironmentName,
                    item.MessageType,
                    item.Name,
                    item.RequestId,
                    item.TimeStamp);
            }
            _eventsTable.AcceptChanges();
        }

        protected override async Task EmitBatchAsync(IEnumerable<EventEntry> events)
        {
            var list = events as IList<EventEntry> ?? events.ToList();

            this.Fill(list);

            using (var connection = new SqlConnection(_options.ConnectionString))
            {
                connection.Open();

                using (var copy = new SqlBulkCopy(connection))
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
            }
            _eventsTable.Clear();
        }

        public Task Append(EventMessage instance)
        {
            Argument.NotNull(instance, nameof(instance));

            this.Emit(new EventEntry(instance, _environment));

            return Task.FromResult(0);
        }

        public async Task<IEnumerable<EventEntry>> GetEvents(DateTimeOffset? start = null, DateTimeOffset? end = null)
        {
            var builder = new StringBuilder($"SELECT TOP {_options.SelectLimit} * FROM {_options.EventsTableName} WHERE Not Id IS NULL");
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
                                .Select(e => new EventEntry
                                {
                                    Id = e["EntryId"].GetValue<string>(),
                                    ApplicationName = e["ApplicationName"].GetValue<string>(),
                                    Body = e["Body"].GetValue<string>(),
                                    EnvironmentName = e["Environment"].GetValue<string>(),
                                    MessageType = e["EventType"].GetValue<string>(),
                                    Name = e["Name"].GetValue<string>(),
                                    RequestId = e["RequestId"].GetValue<string>(),
                                    TimeStamp = e["TimeStamp"].GetValue<DateTimeOffset>(),
                                });
                        }
                    }
                }
            }
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
            table.Columns.Add(new DataColumn("EntryId")
            {
                DataType = typeof(string)
            });
            table.Columns.Add(new DataColumn("ApplicationName")
            {
                DataType = typeof(string)
            });
            table.Columns.Add(new DataColumn("Body")
            {
                DataType = typeof(string)
            });
            table.Columns.Add(new DataColumn("Environment")
            {
                DataType = typeof(string)
            });
            table.Columns.Add(new DataColumn("EventType")
            {
                DataType = typeof(string)
            });
            table.Columns.Add(new DataColumn("Name")
            {
                DataType = typeof(string)
            });
            table.Columns.Add(new DataColumn("RequestId")
            {
                DataType = typeof(string)
            });
            table.Columns.Add(new DataColumn("TimeStamp")
            {
                DataType = typeof(DateTimeOffset)
            });

            return table;
        }
    }
}
