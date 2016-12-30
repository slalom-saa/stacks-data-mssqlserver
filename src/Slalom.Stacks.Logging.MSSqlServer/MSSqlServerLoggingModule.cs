using System;
using Autofac;
using Microsoft.Extensions.Configuration;
using Slalom.Stacks.Messaging.Logging;
using Slalom.Stacks.Configuration;
using Slalom.Stacks.Validation;

namespace Slalom.Stacks.Logging.MSSqlServer
{
    /// <summary>
    /// An Autofac module for the SQL Server Logging module.
    /// </summary>
    /// <seealso cref="Autofac.Module" />
    public class MSSqlServerLoggingModule : Module
    {
        private readonly MsSqlServerLoggingOptions _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="MSSqlServerLoggingModule"/> class.
        /// </summary>
        /// <param name="options">The options to use.</param>
        public MSSqlServerLoggingModule(MsSqlServerLoggingOptions options)
        {
            Argument.NotNull(options, nameof(options));

            _options = options;
        }

        /// <summary>
        /// Override to add registrations to the container.
        /// </summary>
        /// <param name="builder">The builder through which components can be
        /// registered.</param>
        /// <remarks>Note that the ContainerBuilder parameter is unique to this module.</remarks>
        protected override void Load(ContainerBuilder builder)
        {
            base.Load(builder);

            builder.Register(c => new AuditStore(_options.ConnectionString))
                   .AsImplementedInterfaces()
                   .AsSelf();

            builder.Register<ILogStore>(c => new LogStore(_options.ConnectionString))
                   .AsImplementedInterfaces()
                   .AsSelf();
        }
    }
}