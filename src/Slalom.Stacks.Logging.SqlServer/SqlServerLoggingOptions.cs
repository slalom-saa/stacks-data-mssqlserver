using System;
using Serilog.Events;
using Slalom.Stacks.Validation;

namespace Slalom.Stacks.Logging.SqlServer
{
    /// <summary>
    /// Options for the SQL Server Logging module.
    /// </summary>
    public class SqlServerLoggingOptions
    {
        internal string EventsTableName { get; private set; } = "Events";

        internal int BatchSize { get; private set; } = 1000;

        /// <summary>
        /// Gets the connection string.
        /// </summary>
        /// <value>The connection string.</value>
        internal string ConnectionString { get; set; } = "Data Source=localhost;Initial Catalog=Stacks.Logs;Integrated Security=True;MultipleActiveResultSets=True";

        internal LogEventLevel LogLevel { get; private set; } = LogEventLevel.Warning;

        internal string TraceTableName { get; private set; } = "Trace";

        internal TimeSpan Period { get; private set; } = TimeSpan.FromSeconds(5);

        internal string RequestsTableName { get; private set; } = "Requests";

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
            this.LogLevel = level;

            return this;
        }
    }
}