using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Slalom.Stacks.Configuration;
using Slalom.Stacks.Logging.MSSqlServer;

namespace Slalom.Stacks.Configuration
{
    public static class ApplicationContainerExtensions
    {
        public static ApplicationContainer UseSqlServerAuditing(this ApplicationContainer instance)
        {
            instance.RegisterModule(new MSSqlServerLoggingModule());
            return instance;
        }

        public static ApplicationContainer UseSqlServerAuditing(this ApplicationContainer instance, Action<MsSqlServerLoggingOptions> configuration)
        {
            instance.RegisterModule(new MSSqlServerLoggingModule(configuration));
            return instance;
        }
    }
}
