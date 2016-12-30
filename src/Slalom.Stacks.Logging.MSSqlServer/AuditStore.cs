using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Slalom.Stacks.Messaging.Logging;
using Slalom.Stacks.Validation;

namespace Slalom.Stacks.Logging.MSSqlServer
{
    /// <summary>
    /// A SQL Server <see cref="IAuditStore"/> implementation.
    /// </summary>
    /// <seealso cref="Slalom.Stacks.Messaging.Logging.IAuditStore" />
    public class AuditStore : IAuditStore
    {
        private readonly string _connectionString;

        /// <summary>
        /// Initializes a new instance of the <see cref="AuditStore"/> class.
        /// </summary>
        public AuditStore(string connectionString)
        {
            Argument.NotNullOrWhiteSpace(connectionString, nameof(connectionString));

            _connectionString = connectionString;
        }

        /// <summary>
        /// Appends an audit with the specified execution elements.
        /// </summary>
        /// <param name="audit">The audit entry to append.</param>
        /// <returns>A task for asynchronous programming.</returns>
        public async Task AppendAsync(AuditEntry audit)
        {
            Argument.NotNull(audit, nameof(audit));

            var text =
                @"INSERT INTO [Audits] ([ApplicationName], [CorrelationId], [Environment], [EventId], [EventName], [MachineName], [Path], [Payload], [SessionId], [ThreadId], [TimeStamp], [UserHostAddress], [UserName]) VALUES (@p0, @p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8, @p9, @p10, @p11, @p12)";
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var command = new SqlCommand(text, connection))
                {
                    command.Parameters.AddWithValue("p0", (object)audit.ApplicationName ?? DBNull.Value);
                    command.Parameters.AddWithValue("p1", (object)audit.CorrelationId ?? DBNull.Value);
                    command.Parameters.AddWithValue("p2", (object)audit.Environment ?? DBNull.Value);
                    command.Parameters.AddWithValue("p3", (object)audit.EventId ?? DBNull.Value);
                    command.Parameters.AddWithValue("p4", (object)audit.EventName ?? DBNull.Value);
                    command.Parameters.AddWithValue("p5", (object)audit.MachineName ?? DBNull.Value);
                    command.Parameters.AddWithValue("p6", (object)audit.Path ?? DBNull.Value);
                    command.Parameters.AddWithValue("p7", (object)audit.Payload ?? DBNull.Value);
                    command.Parameters.AddWithValue("p8", (object)audit.SessionId ?? DBNull.Value);
                    command.Parameters.AddWithValue("p9", (object)audit.ThreadId ?? DBNull.Value);
                    command.Parameters.AddWithValue("p10", (object)audit.TimeStamp ?? DBNull.Value);
                    command.Parameters.AddWithValue("p11", (object)audit.UserHostAddress ?? DBNull.Value);
                    command.Parameters.AddWithValue("p12", (object)audit.UserName ?? DBNull.Value);

                    await command.ExecuteNonQueryAsync();
                }
            }
        }
    }
}