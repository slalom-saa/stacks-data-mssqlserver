using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using ConsoleClient.Commands;
using ConsoleClient.Commands.AddItem;
using Slalom.Stacks.Configuration;
using Slalom.Stacks.Logging.MSSqlServer;

// ReSharper disable AccessToDisposedClosure

#pragma warning disable 1998
#pragma warning disable 4014

namespace ConsoleClient
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Task.Run(() => new Program().Start());
            Console.WriteLine("Running application.  Press any key to halt...");
            Console.ReadKey();
        }

        public async Task Start()
        {
            try
            {
                var watch = new Stopwatch();
                using (var container = new ApplicationContainer(this))
                {
                    container.UseSqlServerAuditing();

                    watch.Start();
                    for (int i = 0; i < 100; i++)
                    {
                        var local = i;
                        await Task.Run(() => container.Bus.SendAsync(new AddItemCommand("test " + local)).ConfigureAwait(false));
                    }
                    watch.Stop();
                }
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Execution completed successfully in {watch.Elapsed}.  Press any key to exit...");
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