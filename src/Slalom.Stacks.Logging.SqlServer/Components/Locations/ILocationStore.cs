using System.Threading.Tasks;

namespace Slalom.Stacks.Logging.SqlServer.Components.Locations
{
    public interface ILocationStore
    {
        Task Append(params string[] addresses);
    }
}