/* 
 * Copyright (c) Stacks Contributors
 * 
 * This file is subject to the terms and conditions defined in
 * the LICENSE file, which is part of this source code package.
 */

using Microsoft.Extensions.Configuration;
using Slalom.Stacks.Services;

namespace Slalom.Stacks.Logging.SqlServer.EndPoints
{
    /// <summary>
    /// Gets the SQL Logging configuration.
    /// </summary>
    [EndPoint("_system/configuration/sql-logging", Method = "GET", Name = "Get SQL Sever Logging Configuration", Public = false)]
    public class GetConfiguration : EndPoint<GetConfigurationRequest, SqlServerLoggingOptions>
    {
        private readonly IConfiguration _configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetConfiguration" /> class.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        public GetConfiguration(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <inheritdoc />
        public override SqlServerLoggingOptions Receive(GetConfigurationRequest instance)
        {
            var options = new SqlServerLoggingOptions();

            _configuration.GetSection("Stacks:Logging:SqlServer").Bind(options);

            return options;
        }
    }
}