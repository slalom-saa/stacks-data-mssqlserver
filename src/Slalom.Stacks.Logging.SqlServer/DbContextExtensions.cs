using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Slalom.Stacks.Logging.SqlServer
{
    /// <summary>
    /// Extensions for <see cref="DbContext"/> classes.
    /// </summary>
    public static class DbContextExtensions
    {
        /// <summary>
        /// Ensures that the database is created and no migrations are pending.
        /// </summary>
        /// <param name="context">The context.</param>
        public static void EnsureMigrations(this DbContext context)
        {
            context.Database.EnsureCreated();
            if (context.Database.GetPendingMigrations().Any())
            {
                context.Database.Migrate();
            }
        }

        /// <summary>
        /// Ensures that the database is created and no migrations are pending.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns>Task.</returns>
        public static async Task EnsureMigrationsAsync(this DbContext context)
        {
            await context.Database.EnsureCreatedAsync();
            if ((await context.Database.GetPendingMigrationsAsync()).Any())
            {
                await context.Database.MigrateAsync();
            }
        }
    }
}