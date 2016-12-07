using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Slalom.Stacks.Communication;
using Slalom.Stacks.Communication.Logging;
using Slalom.Stacks.Runtime;
using Slalom.Stacks.Validation;

namespace Slalom.Stacks.Logging.MSSqlServer
{
    /// <summary>
    /// A SQL Server <see cref="IAuditStore"/> implementation.
    /// </summary>
    /// <seealso cref="Slalom.Stacks.Communication.Logging.IAuditStore" />
    public class AuditStore : IAuditStore
    {
        private readonly DbContext _context;

        /// <summary>
        /// Initializes a new instance of the <see cref="AuditStore"/> class.
        /// </summary>
        /// <param name="context">The context.</param>
        public AuditStore(DbContext context)
        {
            Argument.NotNull(() => context);

            _context = context;
        }

        /// <summary>
        /// Appends an audit with the specified execution elements.
        /// </summary>
        /// <param name="event">The raised event.</param>
        /// <param name="context">The current <see cref="T:Slalom.Boost.Commands.CommandContext" /> instance.</param>
        /// <returns>A task for asynchronous programming.</returns>
        public Task AppendAsync(IEvent @event, ExecutionContext context)
        {
            _context.Add(new Audit(@event, context));

            return _context.SaveChangesAsync();
        }
    }
}
