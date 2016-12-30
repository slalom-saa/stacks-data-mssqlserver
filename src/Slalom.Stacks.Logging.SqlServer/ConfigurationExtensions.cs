using System;
using Slalom.Stacks.Configuration;
using Slalom.Stacks.Validation;

namespace Slalom.Stacks.Logging.SqlServer
{
    /// <summary>
    /// Contains extension methods to add SQL Server Logging blocks.
    /// </summary>
    public static class ConfigurationExtensions
    {
        /// <summary>
        /// Adds the SQL Server Auditing block to the container.
        /// </summary>
        /// <param name="instance">The this instance.</param>
        /// <param name="configuration">The configuration routine.</param>
        /// <returns>Returns the container instance for method chaining.</returns>
        public static ApplicationContainer UseSqlServerAuditing(this ApplicationContainer instance, Action<SqlServerLoggingOptions> configuration = null)
        {
            Argument.NotNull(instance, nameof(instance));

            var options = new SqlServerLoggingOptions();

            configuration?.Invoke(options);

            instance.RegisterModule(new SqlServerLoggingModule(options));
            return instance;
        }
    }
}