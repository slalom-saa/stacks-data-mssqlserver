using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Slalom.Stacks.Communication.Logging;

namespace Slalom.Stacks.Logging.MSSqlServer
{
    public class LoggingDbContext : DbContext
    {
        private readonly string _connectionString;

        public LoggingDbContext(string connectionString)
        {
            _connectionString = connectionString;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);

            optionsBuilder.UseSqlServer(_connectionString);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Log>()
                        .ToTable("Logs")
                        .HasKey(e => e.Id);

            modelBuilder.Entity<Audit>()
                        .ToTable("Audits")
                        .HasKey(e => e.Id);
        }
    }
}
