using System;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Slalom.Stacks.Logging.SqlServer;
using Slalom.Stacks.Messaging.Logging;
using Slalom.Stacks.Test.Examples;

namespace Slalom.Stacks.ConsoleClient
{
    public class Program
    {
        public static void Main(string[] args)
        {
           var runner = new ExampleRunner();
            runner.With(e => e.UseSqlServerAuditing());
            runner.Start();
            
            Console.WriteLine("Running application.  Press any key to halt...");
            Console.ReadKey();
        }
    }
}