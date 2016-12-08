using System;
using System.Linq;
using Slalom.Stacks.Logging.MSSqlServer;
using Slalom.Stacks.Validation;

namespace Slalom.Stacks.Configuration
{
    /// <summary>
    /// Contains configuration methods for the SQL Server Logging block.
    /// </summary>
    public static class ConfigurationExtensions
    {
        /// <summary>
        /// Adds the SQL Server Auditing block to the container.
        /// </summary>
        /// <param name="instance">The this instance.</param>
        /// <returns>Returns the container instance for method chaining.</returns>
        public static ApplicationContainer UseSqlServerAuditing(this ApplicationContainer instance)
        {
            Argument.NotNull(() => instance);

            instance.RegisterModule(new MSSqlServerLoggingModule());
            return instance;
        }

        /// <summary>
        /// Adds the SQL Server Auditing block to the container.
        /// </summary>
        /// <param name="instance">The this instance.</param>
        /// <param name="configuration">The configuration routine.</param>
        /// <returns>Returns the container instance for method chaining.</returns>
        public static ApplicationContainer UseSqlServerAuditing(this ApplicationContainer instance, Action<MsSqlServerLoggingOptions> configuration)
        {
            Argument.NotNull(() => instance);

            instance.RegisterModule(new MSSqlServerLoggingModule(configuration));
            return instance;
        }
    }
}