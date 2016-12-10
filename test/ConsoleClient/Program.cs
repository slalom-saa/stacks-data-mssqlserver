using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ConsoleClient.Commands;
using Slalom.Stacks.Configuration;
using Slalom.Stacks.Logging.MSSqlServer;

#pragma warning disable 1998
#pragma warning disable 4014

namespace ConsoleClient
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Task.Factory.StartNew(() => new Program().Start());
            Console.WriteLine("Running application.  Press any key to halt...");
            Console.ReadKey();
        }

        public async Task Start()
        {
            try
            {
                using (var container = new ApplicationContainer(this))
                {
                    container.UseSqlServerAuditing();

                    var tasks = new List<Task>();
                    for (int i = 0; i < 100; i++)
                    {
                        tasks.Add(container.Bus.Send(new TestCommand()));
                    }

                    await Task.WhenAll(tasks);
                }
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Execution completed successfully.  Press any key to exit...");
                Console.ResetColor();
            }
            catch (Exception exception)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(exception);
                Console.ResetColor();
            }
        }
    }
}