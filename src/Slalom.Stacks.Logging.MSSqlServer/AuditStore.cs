using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Slalom.Stacks.Communication;
using Slalom.Stacks.Communication.Logging;
using Slalom.Stacks.Runtime;

namespace Slalom.Stacks.Logging.MSSqlServer
{
    public class AuditStore : IAuditStore
    {
        private readonly DbContext _context;

        public AuditStore(DbContext context)
        {
            _context = context;
        }

        public Task AppendAsync(IEvent @event, ExecutionContext context)
        {
            _context.Add(new Audit(@event, context));
            return _context.SaveChangesAsync();
        }
    }
}
