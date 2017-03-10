using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Slalom.Stacks.Logging;
using Slalom.Stacks.Logging.SqlServer;
using Slalom.Stacks.Messaging;
using Slalom.Stacks.Messaging.Logging;
using Slalom.Stacks.Runtime;
using Slalom.Stacks.Services;

namespace Slalom.Stacks.ConsoleClient
{

    public class AddCommand : Command
    {
    }

    public class Add : UseCase<AddCommand>
    {
        public override void Execute(AddCommand command)
        {
            throw new Exception("Af");
            Console.WriteLine("...");
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

                    stack.Send("", new AddCommand()).Wait();
                }
            }
            catch(Exception exception)
            {
                Console.WriteLine(exception);
            }

        }
    }

}