using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Newtonsoft.Json;
using Slalom.Stacks.Logging;
using Slalom.Stacks.Logging.SqlServer;
using Slalom.Stacks.Runtime;
using Slalom.Stacks.Services;

namespace Slalom.Stacks.ConsoleClient
{

    [EndPoint("go")]
    public class End : EndPoint
    {
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                using (var stack = new Stack(typeof(Program)))
                {
                    stack.UseSqlServerLogging();

                    var logger = stack.Container.Resolve<ILogger>();

                    stack.Send("go").Wait();
                    

                    logger.Debug("adf");
                    //stack.Send("", new AddCommand()).Wait();

                    Console.WriteLine("...");
                    Console.ReadKey();
                }
            }
            catch(Exception exception)
            {
                Console.WriteLine(exception);
            }

        }
    }

}