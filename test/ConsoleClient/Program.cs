using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Slalom.Stacks.Configuration;
using Slalom.Stacks.Logging.MSSqlServer;
using Slalom.Stacks.Test.Commands.AddItem;
using Slalom.Stacks.Test.Domain;
using Slalom.Stacks.Test.Search;

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
                var count = 1000;
                using (var container = new ApplicationContainer(typeof(Item)))
                {
                    container.UseSqlServerAuditing();

                    watch.Start();

                    var tasks = new List<Task>(count);
                    Parallel.For(0, count, new ParallelOptions { MaxDegreeOfParallelism = 4 }, e =>
                    {
                        tasks.Add(container.Bus.SendAsync(new AddItemCommand(DateTime.Now.Ticks.ToString())));
                    });
                    await Task.WhenAll(tasks);

                    watch.Stop();

                    var searchResultCount = container.Search.OpenQuery<ItemSearchResult>().Count();
                    var entityCount = container.Domain.OpenQuery<Item>().Count();
                    if (searchResultCount != count || entityCount != count)
                    {
                        throw new Exception($"The execution did not have the expected results. {searchResultCount} search results and {entityCount} entities out of {count}.");
                    }
                }

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Execution for {count:N0} items completed successfully in {watch.Elapsed} - {Math.Ceiling(count / watch.Elapsed.TotalSeconds):N0} per second.  Press any key to exit...");
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