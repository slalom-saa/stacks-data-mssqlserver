using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Slalom.Stacks.Communication.Logging;

namespace Slalom.Stacks.Logging.MSSqlServer
{
    public class MSSqlServerLoggingOptions
    {
        public string ConnectionString { get; set; } = "Data Source=localhost;Initial Catalog=Stacks.Logs;Integrated Security=True";
    }

    public class MSSqlServerLoggingModule : Module
    {
        private readonly MSSqlServerLoggingOptions _options = new MSSqlServerLoggingOptions();

        public MSSqlServerLoggingModule()
        {
        }

        public MSSqlServerLoggingModule(Action<MSSqlServerLoggingOptions> configuration)
        {
            configuration(_options);
        }

        protected override void Load(ContainerBuilder builder)
        {
            base.Load(builder);

            builder.Register(c =>
            {
                var context = new LoggingDbContext(_options.ConnectionString);
                context.EnsureMigrations();
                return context;
            }).SingleInstance();

            builder.Register<IAuditStore>(c => new AuditStore(c.Resolve<LoggingDbContext>()))
                .SingleInstance();

            builder.Register<ILogStore>(c => new LogStore(c.Resolve<LoggingDbContext>()))
                .SingleInstance();
        }
    }
}
