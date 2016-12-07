using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Slalom.Stacks.Communication;
using Slalom.Stacks.Communication.Logging;
using Slalom.Stacks.Runtime;
using Slalom.Stacks.Validation;

namespace Slalom.Stacks.Logging.MSSqlServer
{
    /// <summary>
    /// A SQL Server <see cref="ILogStore"/> implementation.
    /// </summary>
    /// <seealso cref="Slalom.Stacks.Communication.Logging.ILogStore" />
    public class LogStore : ILogStore
    {
        private readonly DbContext _context;

        /// <summary>
        /// Initializes a new instance of the <see cref="LogStore"/> class.
        /// </summary>
        /// <param name="context">The context.</param>
        public LogStore(DbContext context)
        {
            Argument.NotNull(() => context);

            _context = context;
        }

        /// <summary>
        /// Appends an audit with the specified execution elements.
        /// </summary>
        /// <param name="command">The command that was executed.</param>
        /// <param name="result">The execution result.</param>
        /// <param name="context">The current <see cref="T:Slalom.Boost.Commands.CommandContext" /> instance.</param>
        /// <returns>A task for asynchronous programming.</returns>
        public Task AppendAsync(ICommand command, ICommandResult result, ExecutionContext context)
        {
            _context.Add(new Log(command, result, context));

            return _context.SaveChangesAsync();
        }
    }
}