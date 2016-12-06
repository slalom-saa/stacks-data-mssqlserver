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

        public async Task Run()
        {
            using (var container = new ApplicationContainer(this))
            {
                var options = new DbContextOptionsBuilder();
                options.UseSqlServer("Data Source=localhost;Initial Catalog=Logs;Integrated Security=True", a =>
                {
                    a.
                });

                var context = new DbContext(options.Options);

                var audit = new AuditStore(context);

                await audit.AppendAsync(new TestEvent(), new LocalExecutionContext());
            }
            Console.WriteLine("Done executing");
        }
    }
}
