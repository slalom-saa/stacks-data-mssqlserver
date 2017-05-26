﻿// Copyright 2014 Serilog Contributors
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
// Modifications copyright(C) 2017 Stacks Contributors

using System;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Data;
using Serilog;
using Serilog.Configuration;
using Serilog.Debugging;
using Serilog.Events;
using Serilog.Sinks.MSSqlServer;
using Slalom.Stacks.Logging.SqlServer.Components.Locations;

namespace Slalom.Stacks.Logging.SqlServer.Components
{
    /// <summary>
    /// Adds the WriteTo.MSSqlServer() extension method to <see cref="LoggerConfiguration" />.
    /// </summary>
    public static class LoggerConfigurationMSSqlServerExtensions
    {
        /// <summary>
        /// Adds a sink that writes log events to a table in a MSSqlServer database.
        /// Create a database and execute the table creation script found here
        /// https://gist.github.com/mivano/10429656
        /// or use the autoCreateSqlTable option.
        /// </summary>
        /// <param name="loggerConfiguration">The logger configuration.</param>
        /// <param name="connectionString">The connection string to the database where to store the events.</param>
        /// <param name="tableName">Name of the table to store the events in.</param>
        /// <param name="restrictedToMinimumLevel">The minimum log event level required in order to write an event to the sink.</param>
        /// <param name="batchPostingLimit">The maximum number of events to post in a single batch.</param>
        /// <param name="period">The time to wait between checking for event batches.</param>
        /// <param name="formatProvider">Supplies culture-specific formatting information, or null.</param>
        /// <param name="autoCreateSqlTable">Create log table with the provided name on destination sql server.</param>
        /// <param name="columnOptions"></param>
        /// <returns>Logger configuration, allowing configuration to continue.</returns>
        /// <exception cref="ArgumentNullException">A required parameter is null.</exception>
        public static LoggerConfiguration StacksSqlServer(
            this LoggerSinkConfiguration loggerConfiguration,
            string connectionString,
            string tableName,
            LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum,
            int batchPostingLimit = Serilog.Sinks.MSSqlServer.MSSqlServerSink.DefaultBatchPostingLimit,
            TimeSpan? period = null,
            IFormatProvider formatProvider = null,
            bool autoCreateSqlTable = false,
            ColumnOptions columnOptions = null,
            ILocationStore locations = null
        )
        {
            if (loggerConfiguration == null)
            {
                throw new ArgumentNullException("loggerConfiguration");
            }

            var defaultedPeriod = period ?? Serilog.Sinks.MSSqlServer.MSSqlServerSink.DefaultPeriod;

            var serviceConfigSection =
                ConfigurationManager.GetSection("MSSqlServerSettingsSection") as MSSqlServerConfigurationSection;

            // If we have additional columns from config, load them as well
            if (serviceConfigSection != null && serviceConfigSection.Columns.Count > 0)
            {
                if (columnOptions == null)
                {
                    columnOptions = new ColumnOptions();
                }
                GenerateDataColumnsFromConfig(serviceConfigSection, columnOptions);
            }

            connectionString = GetConnectionString(connectionString);

            return loggerConfiguration.Sink(
                new MSSqlServerSink(
                    connectionString,
                    tableName,
                    batchPostingLimit,
                    defaultedPeriod,
                    formatProvider,
                    autoCreateSqlTable,
                    columnOptions,
                    locations
                ),
                restrictedToMinimumLevel);
        }

        /// <summary>
        /// Generate an array of DataColumns using the supplied MSSqlServerConfigurationSection,
        ///     which is an array of keypairs defining the SQL column name and SQL data type
        /// Entries are appended to a list of DataColumns in column options
        /// </summary>
        /// <param name="serviceConfigSection">A previously loaded configuration section</param>
        /// <param name="columnOptions">column options with existing array of columns to append our config columns to</param>
        private static void GenerateDataColumnsFromConfig(MSSqlServerConfigurationSection serviceConfigSection,
            ColumnOptions columnOptions)
        {
            foreach (ColumnConfig c in serviceConfigSection.Columns)
            {
                // Set the type based on the defined SQL type from config
                var column = new DataColumn(c.ColumnName);

                Type dataType = null;

                switch (c.DataType)
                {
                    case "bigint":
                        dataType = Type.GetType("System.Int64");
                        break;
                    case "bit":
                        dataType = Type.GetType("System.Boolean");
                        break;
                    case "char":
                    case "nchar":
                    case "ntext":
                    case "nvarchar":
                    case "text":
                    case "varchar":
                        dataType = Type.GetType("System.String");
                        break;
                    case "date":
                    case "datetime":
                    case "datetime2":
                    case "smalldatetime":
                        dataType = Type.GetType("System.DateTime");
                        break;
                    case "decimal":
                    case "money":
                    case "numeric":
                    case "smallmoney":
                        dataType = Type.GetType("System.Decimal");
                        break;
                    case "float":
                        dataType = Type.GetType("System.Double");
                        break;
                    case "int":
                        dataType = Type.GetType("System.Int32");
                        break;
                    case "real":
                        dataType = Type.GetType("System.Single");
                        break;
                    case "smallint":
                        dataType = Type.GetType("System.Int16");
                        break;
                    case "time":
                        dataType = Type.GetType("System.TimeSpan");
                        break;
                    case "uniqueidentifier":
                        dataType = Type.GetType("System.Guid");
                        break;
                }
                column.DataType = dataType;
                if (columnOptions.AdditionalDataColumns == null)
                {
                    columnOptions.AdditionalDataColumns = new Collection<DataColumn>();
                }
                columnOptions.AdditionalDataColumns.Add(column);
            }
        }

        /// <summary>
        /// Examine if supplied connection string is a reference to an item in the "ConnectionStrings" section of web.config
        /// If it is, return the ConnectionStrings item, if not, return string as supplied.
        /// </summary>
        /// <param name="nameOrConnectionString">The name of the ConnectionStrings key or raw connection string.</param>
        /// <remarks>Pulled from review of Entity Framework 6 methodology for doing the same</remarks>
        private static string GetConnectionString(string nameOrConnectionString)
        {
            // If there is an `=`, we assume this is a raw connection string not a named value
            // If there are no `=`, attempt to pull the named value from config
            if (nameOrConnectionString.IndexOf('=') < 0)
            {
                var cs = ConfigurationManager.ConnectionStrings[nameOrConnectionString];
                if (cs != null)
                {
                    return cs.ConnectionString;
                }
                SelfLog.WriteLine("MSSqlServer sink configured value {0} is not found in ConnectionStrings settings and does not appear to be a raw connection string.", nameOrConnectionString);
            }

            return nameOrConnectionString;
        }
    }
}