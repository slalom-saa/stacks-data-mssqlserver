using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Slalom.Stacks.Logging.MSSqlServer.Migrations
{
    [DbContext(typeof(LoggingDbContext))]
    partial class LoggingDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
            modelBuilder
                .HasAnnotation("ProductVersion", "1.1.0-rtm-22752")
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            modelBuilder.Entity("Slalom.Stacks.Communication.Logging.Audit", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("ApplicationName");

                    b.Property<string>("CorrelationId");

                    b.Property<string>("Environment");

                    b.Property<Guid?>("EventId");

                    b.Property<string>("EventName");

                    b.Property<string>("Host");

                    b.Property<string>("MachineName");

                    b.Property<string>("Path");

                    b.Property<string>("Payload");

                    b.Property<string>("SessionId");

                    b.Property<int>("ThreadId");

                    b.Property<DateTimeOffset?>("TimeStamp");

                    b.Property<string>("UserHostAddress");

                    b.Property<string>("UserName");

                    b.HasKey("Id");

                    b.ToTable("Audits");
                });

            modelBuilder.Entity("Slalom.Stacks.Communication.Logging.Log", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("ApplicationName");

                    b.Property<Guid>("CommandId");

                    b.Property<string>("CommandName");

                    b.Property<string>("CorrelationId");

                    b.Property<string>("Environment");

                    b.Property<string>("Host");

                    b.Property<bool>("IsSuccessful");

                    b.Property<string>("MachineName");

                    b.Property<string>("Path");

                    b.Property<string>("Payload");

                    b.Property<string>("SessionId");

                    b.Property<int>("ThreadId");

                    b.Property<DateTimeOffset?>("TimeStamp");

                    b.Property<string>("UserHostAddress");

                    b.Property<string>("UserName");

                    b.Property<string>("ValidationErrors");

                    b.HasKey("Id");

                    b.ToTable("Logs");
                });
        }
    }
}
