using System;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Slalom.Stacks.Logging;
using Slalom.Stacks.Logging.SqlServer;
using Slalom.Stacks.Messaging.Logging;
using Slalom.Stacks.Runtime;
using Slalom.Stacks.Test.Examples;

namespace Slalom.Stacks.ConsoleClient
{

    public class Program
    {
        public static void Main(string[] args)
        {
            //var runner = new ExampleRunner();
            //runner.With(e => e.UseSqlServerLogging(o =>
            //{
            //    o.WithTracing(Serilog.Events.LogEventLevel.Verbose);
            //}));
            //runner.Start(2);

            using (var container = new ApplicationContainer())
            {

                var manager = new ContextManager(container.Resolve<IExecutionContextResolver>());


                container.UseSqlServerLogging(o =>
                {
                    o.WithTracing(Serilog.Events.LogEventLevel.Verbose);
                });

                container.Logger.Debug("Hello");
            }


            Console.WriteLine("Running application.  Press any key to halt...");
            Console.ReadKey();
        }
    }
}