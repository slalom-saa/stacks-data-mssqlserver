/* 
 * Copyright (c) Stacks Contributors
 * 
 * This file is subject to the terms and conditions defined in
 * the LICENSE file, which is part of this source code package.
 */

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Slalom.Stacks.Logging.SqlServer.Batching;
using Slalom.Stacks.Logging.SqlServer.Core;
using Slalom.Stacks.Text;

namespace Slalom.Stacks.Logging.SqlServer.Locations
{
    public class LocationBatcher : PeriodicBatcher<string>, ILocationStore
    {
        private readonly IPInformationProvider _provider;
        private readonly SqlServerLoggingOptions _options;

        public LocationBatcher(IPInformationProvider provider, SqlServerLoggingOptions options) : base(options.BatchSize, options.Period)
        {
            _provider = provider;
            _options = options;
        }

        public DataTable CreateTable()
        {
            var table = new DataTable("Locations");

            table.Columns.Add(new DataColumn
            {
                DataType = typeof(int),
                ColumnName = "Id",
                AutoIncrement = true,
                AllowDBNull = false
            });
            table.PrimaryKey = new[] { table.Columns[0] };
            table.Columns.Add(new DataColumn("IPAddress")
            {
                DataType = typeof(string)
            });
            table.Columns.Add(new DataColumn("Latitude")
            {
                DataType = typeof(decimal)
            });
            table.Columns.Add(new DataColumn("Longitude")
            {
                DataType = typeof(decimal)
            });
            table.Columns.Add(new DataColumn("ISP")
            {
                DataType = typeof(string)
            });
            table.Columns.Add(new DataColumn("City")
            {
                DataType = typeof(string)
            });
            table.Columns.Add(new DataColumn("Country")
            {
                DataType = typeof(string)
            });
            table.Columns.Add(new DataColumn("Postal")
            {
                DataType = typeof(string)
            });
            return table;
        }

        protected override Task EmitBatchAsync(IEnumerable<string> events)
        {
            using (var connection = new SqlConnection(_options.ConnectionString))
            {
                connection.Open();

                var current = GetCurrentIPAddresses(connection);

                var added = events.Except(current).Distinct();

                var sql = new StringBuilder();
                foreach (var item in added)
                {
                    try
                    {
                        var info = _provider.Get(item);
                        sql.AppendLine($"INSERT INTO [dbo].[Locations] ([IPAddress],[Latitude],[Longitude],[ISP],[City],[Country],[Postal]) VALUES ('{item}', {info.Latitude}, {info.Longitude}, '{info.Isp.Replace("'", "''")}', '{info.City.Replace("'", "''")}', '{info.Country.Replace("'", "''")}', '{info.Postal.Replace("'", "''")}')");

                    }
                    catch
                    {
                    }
                }
                if (sql.Length > 0)
                {
                    using (var command = new SqlCommand(sql.ToString(), connection))
                    {
                        command.ExecuteNonQuery();
                    }
                }
            }
            return Task.FromResult(0);
        }

        private static IEnumerable<string> GetCurrentIPAddresses(SqlConnection connection)
        {
            using (var command = new SqlCommand("SELECT DISTINCT IPAddress FROM Locations", connection))
            {
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        yield return reader.GetString(0);
                    }
                }
            }
        }


        public Task Append(params string[] addresses)
        {
            foreach (var address in addresses.Where(e => !String.IsNullOrWhiteSpace(e)))
            {
                this.Emit(address);
            }
            return Task.FromResult(0);
        }
    }
 
}