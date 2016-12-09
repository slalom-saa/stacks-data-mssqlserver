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
            new Program().Run();
            Console.WriteLine("Press any key to stop...");
            Console.ReadKey();
        }

        public async Task Run()
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
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
            Console.WriteLine("Done with async execution.");
        }
    }
}