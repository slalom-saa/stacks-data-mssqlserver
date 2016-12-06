using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Slalom.Stacks.Logging.MSSqlServer.Migrations
{
    [DbContext(typeof(LoggingDbContext))]
    public partial class initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Audits",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    ApplicationName = table.Column<string>(nullable: true),
                    CorrelationId = table.Column<string>(nullable: true),
                    Environment = table.Column<string>(nullable: true),
                    EventId = table.Column<Guid>(nullable: true),
                    EventName = table.Column<string>(nullable: true),
                    Host = table.Column<string>(nullable: true),
                    MachineName = table.Column<string>(nullable: true),
                    Path = table.Column<string>(nullable: true),
                    Payload = table.Column<string>(nullable: true),
                    SessionId = table.Column<string>(nullable: true),
                    ThreadId = table.Column<int>(nullable: false),
                    TimeStamp = table.Column<DateTimeOffset>(nullable: true),
                    UserHostAddress = table.Column<string>(nullable: true),
                    UserName = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Audits", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Logs",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    ApplicationName = table.Column<string>(nullable: true),
                    CommandId = table.Column<Guid>(nullable: false),
                    CommandName = table.Column<string>(nullable: true),
                    CorrelationId = table.Column<string>(nullable: true),
                    Environment = table.Column<string>(nullable: true),
                    Host = table.Column<string>(nullable: true),
                    IsSuccessful = table.Column<bool>(nullable: false),
                    MachineName = table.Column<string>(nullable: true),
                    Path = table.Column<string>(nullable: true),
                    Payload = table.Column<string>(nullable: true),
                    SessionId = table.Column<string>(nullable: true),
                    ThreadId = table.Column<int>(nullable: false),
                    TimeStamp = table.Column<DateTimeOffset>(nullable: true),
                    UserHostAddress = table.Column<string>(nullable: true),
                    UserName = table.Column<string>(nullable: true),
                    RaisedException = table.Column<string>(nullable: true),
                    Elapsed = table.Column<TimeSpan>(nullable: true),
                    Started = table.Column<DateTimeOffset>(nullable: true),
                    Completed = table.Column<DateTimeOffset>(nullable: true),
                    ValidationErrors = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Logs", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Audits");

            migrationBuilder.DropTable(
                name: "Logs");
        }
    }
}
