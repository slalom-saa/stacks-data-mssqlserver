using Slalom.Stacks.Validation;

namespace Slalom.Stacks.Logging.MSSqlServer
{
    /// <summary>
    /// Options for the SQL Server Logging module.
    /// </summary>
    public class MsSqlServerLoggingOptions
    {
        /// <summary>
        /// Gets the connection string.
        /// </summary>
        /// <value>The connection string.</value>
        internal string ConnectionString { get; set; } = "Data Source=localhost;Initial Catalog=Stacks.Logs;Integrated Security=True";

        /// <summary>
        /// Sets the connection string to use.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        /// <returns>The instance for chaining.</returns>
        public MsSqlServerLoggingOptions UseConnectionString(string connectionString)
        {
            Argument.NotNullOrWhiteSpace(() => connectionString);

            this.ConnectionString = connectionString;

            return this;
        }
    }
}