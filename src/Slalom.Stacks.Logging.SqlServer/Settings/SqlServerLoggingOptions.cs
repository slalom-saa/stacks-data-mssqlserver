/* 
 * Copyright (c) Stacks Contributors
 * 
 * This file is subject to the terms and conditions defined in
 * the LICENSE file, which is part of this source code package.
 */

using System;
using Serilog.Events;
using Slalom.Stacks.Validation;

namespace Slalom.Stacks.Logging.SqlServer.Settings
{
    /// <summary>
    /// Options for SQL Server Logging.
    /// </summary>
    public class SqlServerLoggingOptions
    {
        /// <summary>
        /// Gets or sets the upper size of the batch to write.  When this number is reached, all items will be written for the given type..
        /// </summary>
        /// <value>The size of the batch used for writing.</value>
        public int BatchSize { get; set; } = 100;

        /// <summary>
        /// Gets the connection string.
        /// </summary>
        /// <value>The connection string.</value>
        public string ConnectionString { get; set; } = "Data Source=.;Initial Catalog=Stacks.Logs;Integrated Security=True;MultipleActiveResultSets=True";

        /// <summary>
        /// Gets or sets a value indicating whether or not a database should be created if not found.
        /// </summary>
        /// <value>Indicates whether or not a database should be created if not found.</value>
        public bool CreateDatabase { get; set; }

        /// <summary>
        /// Gets or sets the name of the table that is used for events.
        /// </summary>
        /// <value>The name of the table that is used for events.</value>
        public string EventsTableName { get; set; } = "Events";

        /// <summary>
        /// Gets or sets the location settings.
        /// </summary>
        /// <value>The location settings.</value>
        public LocationSettings Locations { get; set; } = new LocationSettings();

        /// <summary>
        /// Gets or sets the time between batches.
        /// </summary>
        /// <value>The time between batches.</value>
        public TimeSpan Period { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Gets or sets the name of the table that is used for requests.
        /// </summary>
        /// <value>The name of the table that is used for requests.</value>
        public string RequestsTableName { get; set; } = "Requests";

        /// <summary>
        /// Gets or sets the name of the table that is used for responses.
        /// </summary>
        /// <value>The name of the table that is used for responses.</value>
        public string ResponsesTableName { get; set; } = "Responses";

        /// <summary>
        /// Gets or sets the query selection limit.
        /// </summary>
        /// <value>The query selection limit.</value>
        public int SelectLimit { get; set; } = 1000;

        /// <summary>
        /// Gets the trace log level.
        /// </summary>
        /// <value>The trace log level.</value>
        public string TraceLogLevel { get; set; } = "Warning";

        /// <summary>
        /// Gets or sets the name of the table that is used for traces.
        /// </summary>
        /// <value>The name of the table that is used for traces.</value>
        public string TraceTableName { get; set; } = "Traces";

        /// <summary>
        /// Sets the connection string to use.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        /// <returns>The instance for chaining.</returns>
        public SqlServerLoggingOptions UseConnectionString(string connectionString)
        {
            Argument.NotNullOrWhiteSpace(connectionString, nameof(connectionString));

            this.ConnectionString = connectionString;

            return this;
        }

        /// <summary>
        /// Sets the log level to use for log messages.
        /// </summary>
        /// <param name="level">The log level to use.</param>
        /// <returns>The instance for chaining.</returns>
        public SqlServerLoggingOptions WithTracing(LogEventLevel level)
        {
            this.TraceLogLevel = level.ToString();

            return this;
        }

        internal LogEventLevel GetLogLevel()
        {
            LogEventLevel level;
            if (Enum.TryParse(this.TraceLogLevel, out level))
            {
                return level;
            }
            return LogEventLevel.Warning;
        }
    }
}