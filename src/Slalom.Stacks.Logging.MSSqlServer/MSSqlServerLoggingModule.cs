using System;
using Autofac;
using Microsoft.Extensions.Configuration;
using Slalom.Stacks.Communication.Logging;
using Slalom.Stacks.Validation;

namespace Slalom.Stacks.Logging.MSSqlServer
{
    /// <summary>
    /// An Autofac module for the SQL Server Logging module.
    /// </summary>
    /// <seealso cref="Autofac.Module" />
    public class MSSqlServerLoggingModule : Module
    {
        private readonly MsSqlServerLoggingOptions _options = new MsSqlServerLoggingOptions();

        /// <summary>
        /// Initializes a new instance of the <see cref="MSSqlServerLoggingModule"/> class.
        /// </summary>
        public MSSqlServerLoggingModule()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MSSqlServerLoggingModule"/> class.
        /// </summary>
        /// <param name="configuration">The configuration routine.</param>
        public MSSqlServerLoggingModule(Action<MsSqlServerLoggingOptions> configuration)
        {
            Argument.NotNull(() => configuration);

            configuration(_options);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MSSqlServerLoggingModule"/> class.
        /// </summary>
        /// <param name="options">The options to use.</param>
        public MSSqlServerLoggingModule(MsSqlServerLoggingOptions options)
        {
            Argument.NotNull(() => options);

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

            builder.Register(c =>
            {
                var context = new LoggingDbContext(_options);
                context.EnsureMigrations();
                return context;
            }).SingleInstance()
                   .OnPreparing(e =>
                   {
                       var configuration = e.Context.Resolve<IConfiguration>();
                       _options.ConnectionString = configuration["Stacks:Logging:SQL:ConnectionString"] ?? _options.ConnectionString;
                   });

            builder.Register<IAuditStore>(c => new AuditStore(c.Resolve<LoggingDbContext>()))
                   .SingleInstance();

            builder.Register<ILogStore>(c => new LogStore(c.Resolve<LoggingDbContext>()))
                   .SingleInstance();
        }
    }
}