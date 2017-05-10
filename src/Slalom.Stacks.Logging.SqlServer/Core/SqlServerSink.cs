// Copyright 2013-2016 Serilog Contributors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// Modifications copyright(C) 2017 Slalom Architect Academy

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Serilog.Debugging;
using Serilog.Events;
using Serilog.Formatting.Json;
using Serilog.Sinks.MSSqlServer;
using Serilog.Sinks.PeriodicBatching;
using Slalom.Stacks.Logging.SqlServer.Locations;

namespace Slalom.Stacks.Logging.SqlServer.Core
{
    /// <summary>
    ///     Writes log events as rows in a table of MSSqlServer database.
    /// </summary>
    public class MSSqlServerSink : PeriodicBatchingSink
    {
        /// <summary>
        ///     A reasonable default for the number of events posted in
        ///     each batch.
        /// </summary>
        public const int DefaultBatchPostingLimit = 50;

        /// <summary>
        ///     A reasonable default time to wait between checking for event batches.
        /// </summary>
        public static readonly TimeSpan DefaultPeriod = TimeSpan.FromSeconds(5);

        private readonly HashSet<string> _additionalDataColumnNames;
        private readonly ColumnOptions _columnOptions;

        readonly string _connectionString;

        readonly DataTable _eventsTable;
        readonly IFormatProvider _formatProvider;
        private readonly LocationStore _locations;

        private readonly JsonFormatter _jsonFormatter;
        readonly string _tableName;

        /// <summary>
        ///     Construct a sink posting to the specified database.
        /// </summary>
        /// <param name="connectionString">Connection string to access the database.</param>
        /// <param name="tableName">Name of the table to store the data in.</param>
        /// <param name="batchPostingLimit">The maximum number of events to post in a single batch.</param>
        /// <param name="period">The time to wait between checking for event batches.</param>
        /// <param name="formatProvider">Supplies culture-specific formatting information, or null.</param>
        /// <param name="autoCreateSqlTable">Create log table with the provided name on destination sql server.</param>
        /// <param name="columnOptions">Options that pertain to columns</param>
        public MSSqlServerSink(
            string connectionString,
            string tableName,
            int batchPostingLimit,
            TimeSpan period,
            IFormatProvider formatProvider,
            bool autoCreateSqlTable = false,
            ColumnOptions columnOptions = null,
            LocationStore locations = null
        )
            : base(batchPostingLimit, period)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentNullException("connectionString");

            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentNullException("tableName");

            _connectionString = connectionString;
            _tableName = tableName;
            _formatProvider = formatProvider;
            _locations = locations;
            _columnOptions = columnOptions ?? new ColumnOptions();
            if (_columnOptions.AdditionalDataColumns != null)
                _additionalDataColumnNames = new HashSet<string>(_columnOptions.AdditionalDataColumns.Select(c => c.ColumnName), StringComparer.OrdinalIgnoreCase);

            if (_columnOptions.Store.Contains(StandardColumn.LogEvent))
                _jsonFormatter = new JsonFormatter(formatProvider: formatProvider);

            // Prepare the data table
            _eventsTable = this.CreateDataTable();

            if (autoCreateSqlTable)
            {
                try
                {
                    var tableCreator = new SqlTableCreator(connectionString);
                    tableCreator.CreateTable(_eventsTable);
                }
                catch (Exception ex)
                {
                    SelfLog.WriteLine("Exception {0} caught while creating table {1} to the database specified in the Connection string.", ex, tableName);
                }
            }
        }
        
        /// <summary>
        ///     Disposes the connection
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            if (_eventsTable != null)
                _eventsTable.Dispose();

            base.Dispose(disposing);
        }

        /// <summary>
        ///     Emit a batch of log events, running asynchronously.
        /// </summary>
        /// <param name="events">The events to emit.</param>
        /// <remarks>
        ///     Override either <see cref="PeriodicBatchingSink.EmitBatch" /> or <see cref="PeriodicBatchingSink.EmitBatchAsync" />
        ///     ,
        ///     not both.
        /// </remarks>
        protected override async Task EmitBatchAsync(IEnumerable<LogEvent> events)
        {
            // Copy the events to the data table
            this.FillDataTable(events);

            try
            {
                using (var cn = new SqlConnection(_connectionString))
                {
                    await cn.OpenAsync().ConfigureAwait(false);
                    using (var copy = _columnOptions.DisableTriggers
                                          ? new SqlBulkCopy(cn)
                                          : new SqlBulkCopy(cn, SqlBulkCopyOptions.CheckConstraints | SqlBulkCopyOptions.FireTriggers, null)
                    )
                    {
                        copy.DestinationTableName = _tableName;
                        foreach (var column in _eventsTable.Columns)
                        {
                            var columnName = ((DataColumn)column).ColumnName;
                            var mapping = new SqlBulkCopyColumnMapping(columnName, columnName);
                            copy.ColumnMappings.Add(mapping);
                        }

                        await copy.WriteToServerAsync(_eventsTable).ConfigureAwait(false);
                    }
                }

                await _locations.UpdateAsync(_eventsTable.Rows.OfType<DataRow>().Select(e => e["SourceAddress"].ToString()).Distinct().ToArray()).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                SelfLog.WriteLine("Unable to write {0} log events to the database due to following error: {1}", events.Count(), ex.Message);
            }
            finally
            {
                // Processed the items, clear for the next run
                _eventsTable.Clear();
            }
        }

        /// <summary>
        ///     Mapping values from properties which have a corresponding data row.
        ///     Matching is done based on Column name and property key
        /// </summary>
        /// <param name="row"></param>
        /// <param name="properties"></param>
        private void ConvertPropertiesToColumn(DataRow row, IReadOnlyDictionary<string, LogEventPropertyValue> properties)
        {
            foreach (var property in properties)
            {
                if (!row.Table.Columns.Contains(property.Key))
                    continue;

                var columnName = property.Key;
                var columnType = row.Table.Columns[columnName].DataType;
                object conversion;

                var scalarValue = property.Value as ScalarValue;
                if (scalarValue == null)
                {
                    row[columnName] = property.Value.ToString();
                    continue;
                }

                if (scalarValue.Value == null && row.Table.Columns[columnName].AllowDBNull)
                {
                    row[columnName] = DBNull.Value;
                    continue;
                }

                if (TryChangeType(scalarValue.Value, columnType, out conversion))
                {
                    row[columnName] = conversion;
                }
                else
                {
                    row[columnName] = property.Value.ToString();
                }
            }
        }

        private string ConvertPropertiesToXmlStructure(IEnumerable<KeyValuePair<string, LogEventPropertyValue>> properties)
        {
            var options = _columnOptions.Properties;

            if (options.ExcludeAdditionalProperties)
                properties = properties.Where(p => !_additionalDataColumnNames.Contains(p.Key));

            var sb = new StringBuilder();

            sb.AppendFormat("<{0}>", options.RootElementName);

            foreach (var property in properties)
            {
                var value = XmlPropertyFormatter.Simplify(property.Value, options);
                if (options.OmitElementIfEmpty && string.IsNullOrEmpty(value))
                {
                    continue;
                }

                if (options.UsePropertyKeyAsElementName)
                {
                    sb.AppendFormat("<{0}>{1}</{0}>", XmlPropertyFormatter.GetValidElementName(property.Key), value);
                }
                else
                {
                    sb.AppendFormat("<{0} key='{1}'>{2}</{0}>", options.PropertyElementName, property.Key, value);
                }
            }

            sb.AppendFormat("</{0}>", options.RootElementName);

            return sb.ToString();
        }

        DataTable CreateDataTable()
        {
            var eventsTable = new DataTable(_tableName);

            var id = new DataColumn
            {
                DataType = Type.GetType("System.Int32"),
                ColumnName = !string.IsNullOrWhiteSpace(_columnOptions.Id.ColumnName) ? _columnOptions.Id.ColumnName : "Id",
                AutoIncrement = true
            };
            eventsTable.Columns.Add(id);

            foreach (var standardColumn in _columnOptions.Store)
            {
                switch (standardColumn)
                {
                    case StandardColumn.Level:
                        eventsTable.Columns.Add(new DataColumn
                        {
                            DataType = _columnOptions.Level.StoreAsEnum ? typeof(byte) : typeof(string),
                            MaxLength = _columnOptions.Level.StoreAsEnum ? -1 : 128,
                            ColumnName = _columnOptions.Level.ColumnName ?? StandardColumn.Level.ToString()
                        });
                        break;
                    case StandardColumn.TimeStamp:
                        eventsTable.Columns.Add(new DataColumn
                        {
                            DataType = typeof(DateTime),
                            ColumnName = _columnOptions.TimeStamp.ColumnName ?? StandardColumn.TimeStamp.ToString(),
                            AllowDBNull = false
                        });
                        break;
                    case StandardColumn.LogEvent:
                        eventsTable.Columns.Add(new DataColumn
                        {
                            DataType = typeof(string),
                            ColumnName = _columnOptions.LogEvent.ColumnName ?? StandardColumn.LogEvent.ToString()
                        });
                        break;
                    case StandardColumn.Message:
                        eventsTable.Columns.Add(new DataColumn
                        {
                            DataType = typeof(string),
                            MaxLength = -1,
                            ColumnName = _columnOptions.Message.ColumnName ?? StandardColumn.Message.ToString()
                        });
                        break;
                    case StandardColumn.MessageTemplate:
                        eventsTable.Columns.Add(new DataColumn
                        {
                            DataType = typeof(string),
                            MaxLength = -1,
                            ColumnName = _columnOptions.MessageTemplate.ColumnName ?? StandardColumn.MessageTemplate.ToString()
                        });
                        break;
                    case StandardColumn.Exception:
                        eventsTable.Columns.Add(new DataColumn
                        {
                            DataType = typeof(string),
                            MaxLength = -1,
                            ColumnName = _columnOptions.Exception.ColumnName ?? StandardColumn.Exception.ToString()
                        });
                        break;
                    case StandardColumn.Properties:
                        eventsTable.Columns.Add(new DataColumn
                        {
                            DataType = typeof(string),
                            MaxLength = -1,
                            ColumnName = _columnOptions.Properties.ColumnName ?? StandardColumn.Properties.ToString()
                        });
                        break;
                }
            }

            if (_columnOptions.AdditionalDataColumns != null)
            {
                eventsTable.Columns.AddRange(_columnOptions.AdditionalDataColumns.ToArray());
            }

            eventsTable.Columns.Add(new DataColumn("CorrelationId")
            {
                DataType = typeof(string)
            });
            eventsTable.Columns.Add(new DataColumn("ApplicationName")
            {
                DataType = typeof(string)
            });
            eventsTable.Columns.Add(new DataColumn("EnvironmentName")
            {
                DataType = typeof(string)
            });
            eventsTable.Columns.Add(new DataColumn("MachineName")
            {
                DataType = typeof(string)
            });
            eventsTable.Columns.Add(new DataColumn("ThreadId")
            {
                DataType = typeof(string)
            });

            eventsTable.Columns.Add(new DataColumn
            {
                DataType = typeof(string),
                MaxLength = -1,
                ColumnName = "SourceAddress"
            });

            eventsTable.Columns.Add(new DataColumn
            {
                DataType = typeof(string),
                MaxLength = -1,
                ColumnName = "UserName"
            });

            eventsTable.Columns.Add(new DataColumn
            {
                DataType = typeof(string),
                MaxLength = -1,
                ColumnName = "SessionId"
            });

            // Create an array for DataColumn objects.
            var keys = new DataColumn[1];
            keys[0] = id;
            eventsTable.PrimaryKey = keys;

            return eventsTable;
        }

        void FillDataTable(IEnumerable<LogEvent> events)
        {
            // Add the new rows to the collection. 
            foreach (var logEvent in events)
            {
                var row = _eventsTable.NewRow();

                foreach (var column in _columnOptions.Store)
                {
                    switch (column)
                    {
                        case StandardColumn.Message:
                            row[_columnOptions.Message.ColumnName ?? "Message"] = logEvent.RenderMessage(_formatProvider);
                            break;
                        case StandardColumn.MessageTemplate:
                            row[_columnOptions.MessageTemplate.ColumnName ?? "MessageTemplate"] = logEvent.MessageTemplate;
                            break;
                        case StandardColumn.Level:
                            row[_columnOptions.Level.ColumnName ?? "Level"] = logEvent.Level;
                            break;
                        case StandardColumn.TimeStamp:
                            row[_columnOptions.TimeStamp.ColumnName ?? "TimeStamp"] = _columnOptions.TimeStamp.ConvertToUtc ? logEvent.Timestamp.DateTime.ToUniversalTime() : logEvent.Timestamp.DateTime;
                            break;
                        case StandardColumn.Exception:
                            row[_columnOptions.Exception.ColumnName ?? "Exception"] = logEvent.Exception != null ? logEvent.Exception.ToString() : null;
                            break;
                        case StandardColumn.Properties:
                            row[_columnOptions.Properties.ColumnName ?? "Properties"] = this.ConvertPropertiesToXmlStructure(logEvent.Properties);
                            break;
                        case StandardColumn.LogEvent:
                            row[_columnOptions.LogEvent.ColumnName ?? "LogEvent"] = this.LogEventToJson(logEvent);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                if (_columnOptions.AdditionalDataColumns != null)
                {
                    this.ConvertPropertiesToColumn(row, logEvent.Properties);
                }

                SetColumnValue(logEvent, row, "UserName");
                SetColumnValue(logEvent, row, "CorrelationId");
                SetColumnValue(logEvent, row, "SourceAddress");
                SetColumnValue(logEvent, row, "ApplicationName");
                SetColumnValue(logEvent, row, "EnvironmentName");
                SetColumnValue(logEvent, row, "MachineName");
                SetColumnValue(logEvent, row, "ThreadId");
                SetColumnValue(logEvent, row, "SessionId");

                _eventsTable.Rows.Add(row);
            }

            _eventsTable.AcceptChanges();
        }

        private static void SetColumnValue(LogEvent logEvent, DataRow row, string name)
        {
            if (logEvent.Properties.Keys.Contains(name))
            {
                row[name] = ((ScalarValue)logEvent.Properties[name]).Value;
            }
        }

        private string LogEventToJson(LogEvent logEvent)
        {
            if (_columnOptions.LogEvent.ExcludeAdditionalProperties)
            {
                var filteredProperties = logEvent.Properties.Where(p => !_additionalDataColumnNames.Contains(p.Key));
                logEvent = new LogEvent(logEvent.Timestamp, logEvent.Level, logEvent.Exception, logEvent.MessageTemplate, filteredProperties.Select(x => new LogEventProperty(x.Key, x.Value)));
            }

            var sb = new StringBuilder();
            using (var writer = new StringWriter(sb))
            {
                _jsonFormatter.Format(logEvent, writer);
            }
            return sb.ToString();
        }

        /// <summary>
        ///     Try to convert the object to the given type
        /// </summary>
        /// <param name="obj">object</param>
        /// <param name="type">type to convert to</param>
        /// <param name="conversion">result of the converted value</param>        
        private static bool TryChangeType(object obj, Type type, out object conversion)
        {
            conversion = null;
            try
            {
                conversion = Convert.ChangeType(obj, type);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}