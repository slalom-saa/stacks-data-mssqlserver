using System;
using System.Threading.Tasks;

namespace Slalom.Stacks.Logging.SqlServer.Components.Locations
{
    public class NullLocationStore : ILocationStore
    {
        public Task Append(params string[] addresses)
        {
            return Task.FromResult(0);
        }
    }
}
