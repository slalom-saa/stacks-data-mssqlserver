using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Slalom.Stacks.Communication;
using Slalom.Stacks.Configuration;
using Slalom.Stacks.Logging.MSSqlServer;
using Slalom.Stacks.Runtime;

#pragma warning disable 1998
#pragma warning disable 4014

namespace ConsoleClient
{
    public class Program
    {
        public static void Main(string[] args)
        {
            new Program().Run();
            Console.WriteLine("Press any key to stop...");
            Console.ReadKey();
        }

        public class TestEvent : Event
        {
        }

        public class TestCommand : Command<TestEvent>
        {
        }

        public class TestCommandHandler : CommandHandler<TestCommand, TestEvent>
        {
            public override Task<TestEvent> Handle(TestCommand command)
            {
                return Task.FromResult(new TestEvent());
            }
        }

        public async Task Run()
        {
            try
            {
                using (var container = new ApplicationContainer(this))
                {
                    container.RegisterModule(new MSSqlServerLoggingModule());

                    await container.Bus.Send(new TestCommand());
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
            Console.WriteLine("Done executing");
        }
    }
}
