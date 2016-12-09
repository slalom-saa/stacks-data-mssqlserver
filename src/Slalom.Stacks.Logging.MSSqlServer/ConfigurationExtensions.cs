using System;
using Slalom.Stacks.Configuration;
using Slalom.Stacks.Validation;

namespace Slalom.Stacks.Logging.MSSqlServer
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

        /// <summary>
        /// Adds the SQL Server Auditing block to the container.
        /// </summary>
        /// <param name="instance">The this instance.</param>
        /// <param name="options">The options to use.</param>
        /// <returns>Returns the container instance for method chaining.</returns>
        public static ApplicationContainer UseSqlServerAuditing(this ApplicationContainer instance, MsSqlServerLoggingOptions options)
        {
            Argument.NotNull(() => instance);

            instance.RegisterModule(new MSSqlServerLoggingModule(options));
            return instance;
        }
    }
}