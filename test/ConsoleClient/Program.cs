using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Slalom.Stacks.Logging;
using Slalom.Stacks.Logging.SqlServer;
using Slalom.Stacks.Messaging.Logging;
using Slalom.Stacks.Runtime;
using Slalom.Stacks.Test.Examples;
using Slalom.Stacks.Test.Examples.Actors.Items.Add;
using Slalom.Stacks.Test.Examples.Actors.Items.Search;
using ExecutionContext = Slalom.Stacks.Runtime.ExecutionContext;

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

            using (var container = new ApplicationContainer(typeof(AddItemCommand)))
            {
                container.UseSqlServerLogging(o =>
                {
                    o.WithTracing(Serilog.Events.LogEventLevel.Verbose);
                });

                Task.Run(() => new UserRunner(container, "user@stacks.io", "User").Start());
                Task.Run(() => new UserRunner(container, "admin@stacks.io", "Administrator", "173.194.202.100").Start());
                Task.Run(() => new UserRunner(container, "support@stacks.io", "Support", "13.107.21.200").Start());

                Console.WriteLine("...");
                Console.ReadLine();
            }


            Console.WriteLine("Running application.  Press any key to halt...");
            Console.ReadKey();
        }
    }

    public class RunnerExecutionContextResolver : IExecutionContextResolver
    {
        private readonly string _userName;
        private readonly string _role;
        private readonly string _sourceAddress;

        public RunnerExecutionContextResolver(string userName, string role, string sourceAddress)
        {
            _userName = userName;
            _role = role;
            _sourceAddress = sourceAddress;
        }

        public ExecutionContext Resolve()
        {
            return new ExecutionContext("Runner", "Local", "", "", "", new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, _userName), new Claim(ClaimTypes.Role, _role) })), _sourceAddress, "", Environment.CurrentManagedThreadId);
        }
    }

    public class UserRunner
    {
        private static readonly Random _random = new Random(DateTime.Now.Millisecond);

        public UserRunner(ApplicationContainer container, string userName, string role, string sourceAddress = "71.197.137.82")
        {
            this.Container = container.Copy();
            this.Container.Register<IExecutionContextResolver>(c => new RunnerExecutionContextResolver(userName, role, sourceAddress));
        }

        public void Start()
        {
            while (true)
            {
                var next = Convert.ToInt32(((Math.Pow(2 * _random.NextDouble() - 1, 3) / 2) + .5) * 10);

                if (_random.Next(5) % 5 == 0)
                {
                    this.Container.Commands.SendAsync(new AddItemCommand("Product " + next));
                }
                else
                {
                   
                    this.Container.Commands.SendAsync(new SearchItemsCommand(next.ToString()));
                }
                Thread.Sleep(_random.Next(200));
            }
        }

        public ApplicationContainer Container { get; set; }
    }
}