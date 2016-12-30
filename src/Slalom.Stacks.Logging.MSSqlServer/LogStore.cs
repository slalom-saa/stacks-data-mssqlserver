using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Slalom.Stacks.Messaging.Logging;
using Slalom.Stacks.Validation;

namespace Slalom.Stacks.Logging.MSSqlServer
{
    /// <summary>
    /// A SQL Server <see cref="ILogStore"/> implementation.
    /// </summary>
    /// <seealso cref="Slalom.Stacks.Messaging.Logging.ILogStore" />
    public class LogStore : ILogStore
    {
        private readonly string _connectionString;

        /// <summary>
        /// Initializes a new instance of the <see cref="LogStore" /> class.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        public LogStore(string connectionString)
        {
            Argument.NotNullOrWhiteSpace(connectionString, nameof(connectionString));

            _connectionString = connectionString;
        }

        /// <summary>
        /// Appends an audit with the specified execution elements.
        /// </summary>
        /// <param name="entry">The log entry to append.</param>
        /// <returns>A task for asynchronous programming.</returns>
        public async Task AppendAsync(LogEntry entry)
        {
            Argument.NotNull(entry, nameof(entry));

            var text =
                @"INSERT INTO [Logs] ([ApplicationName], [CommandId], [CommandName], [Completed], [CorrelationId], [Elapsed], [Environment], [Exception], [IsSuccessful], [MachineName], [Path], [Payload], [SessionId], [Started], [ThreadId], [TimeStamp], [UserHostAddress], [UserName], [ValidationErrors]) VALUES (@p0, @p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8, @p9, @p10, @p11, @p12, @p13, @p14, @p15, @p16, @p17, @p18)";
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var command = new SqlCommand(text, connection))
                {
                    command.Parameters.AddWithValue("p0", (object)entry.ApplicationName ?? DBNull.Value);
                    command.Parameters.AddWithValue("p1", (object)entry.CommandId ?? DBNull.Value);
                    command.Parameters.AddWithValue("p2", (object)entry.CommandName ?? DBNull.Value);
                    command.Parameters.AddWithValue("p3", (object)entry.Completed ?? DBNull.Value);
                    command.Parameters.AddWithValue("p4", (object)entry.CorrelationId ?? DBNull.Value);
                    command.Parameters.AddWithValue("p5", (object)entry.Elapsed ?? DBNull.Value);
                    command.Parameters.AddWithValue("p6", (object)entry.Environment ?? DBNull.Value);
                    command.Parameters.AddWithValue("p7", (object)entry.RaisedException?.ToString() ?? DBNull.Value);
                    command.Parameters.AddWithValue("p8", (object)entry.IsSuccessful ?? DBNull.Value);
                    command.Parameters.AddWithValue("p9", (object)entry.MachineName ?? DBNull.Value);
                    command.Parameters.AddWithValue("p10", (object)entry.Path ?? DBNull.Value);
                    command.Parameters.AddWithValue("p11", (object)entry.Payload ?? DBNull.Value);
                    command.Parameters.AddWithValue("p12", (object)entry.SessionId ?? DBNull.Value);
                    command.Parameters.AddWithValue("p13", (object)entry.Started ?? DBNull.Value);
                    command.Parameters.AddWithValue("p14", (object)entry.ThreadId ?? DBNull.Value);
                    command.Parameters.AddWithValue("p15", (object)entry.TimeStamp ?? DBNull.Value);
                    command.Parameters.AddWithValue("p16", (object)entry.UserHostAddress ?? DBNull.Value);
                    command.Parameters.AddWithValue("p17", (object)entry.UserName ?? DBNull.Value);
                    command.Parameters.AddWithValue("p18", (object)entry.ValidationErrors ?? DBNull.Value);

                    await command.ExecuteNonQueryAsync();
                }
            }
        }
    }
}