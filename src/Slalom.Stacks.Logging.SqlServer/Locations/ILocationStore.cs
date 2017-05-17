﻿using System.Threading.Tasks;

namespace Slalom.Stacks.Logging.SqlServer.Locations
{
    public interface ILocationStore
    {
        Task UpdateAsync(params string[] addresses);
    }
}