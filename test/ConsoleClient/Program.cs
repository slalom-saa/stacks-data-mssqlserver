using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Newtonsoft.Json;
using Slalom.Stacks.Logging;
using Slalom.Stacks.Logging.SqlServer;
using Slalom.Stacks.Logging.SqlServer.Locations;
using Slalom.Stacks.Services;
using Slalom.Stacks.Services.Logging;
using Slalom.Stacks.Text;

namespace Slalom.Stacks.ConsoleClient
{

    public class AD : Event
    {
        public string Content { get; set; } = "s";
    }

    [EndPoint("go")]
    public class End : EndPoint
    {
        public override void Receive()
        {
            this.AddRaisedEvent(new AD());
        }
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

                    stack.Logger.Debug("hello");

                    Console.ReadKey();

                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }

        }
    }

}