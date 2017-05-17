using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Slalom.Stacks.Logging.SqlServer.Locations
{
    public class NullLocationStore : ILocationStore
    {
        public Task UpdateAsync(params string[] addresses)
        {
            return Task.FromResult(0);
        }
    }
}
