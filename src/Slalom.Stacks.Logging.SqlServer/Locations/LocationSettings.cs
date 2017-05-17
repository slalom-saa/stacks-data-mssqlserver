using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Slalom.Stacks.Logging.SqlServer.Locations
{
    public class LocationSettings
    {
        public bool On { get; set; } = false;

        public int UserId { get; set; }

        public string Key { get; set; }
    }
}
