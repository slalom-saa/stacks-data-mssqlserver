using System;
using Slalom.Stacks.Validation;

namespace Slalom.Stacks.Logging.SqlServer
{
    /// <summary>
    /// Options for the SQL Server Logging module.
    /// </summary>
    public class SqlServerLoggingOptions
    {
        /// <summary>
        /// Gets the connection string.
        /// </summary>
        /// <value>The connection string.</value>
        internal string ConnectionString { get; set; } = "Data Source=localhost;Initial Catalog=Stacks.Logs;Integrated Security=True;MultipleActiveResultSets=True";

        public int BatchSize { get; private set; } = 1000;

        public TimeSpan Period { get; private set; } = TimeSpan.FromSeconds(5);

        public string LogTableName { get; private set; } = "Logs";

        public string AuditTableName { get; private set; } = "Audits";

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
    }
} 