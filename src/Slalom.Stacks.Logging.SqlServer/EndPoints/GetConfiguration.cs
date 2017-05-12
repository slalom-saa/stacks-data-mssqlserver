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
    [EndPoint("_system/configuration/sql-logging")]
    public class GetConfiguration : EndPoint
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
        public override void Receive()
        {
            var options = new SqlServerLoggingOptions();
            _configuration.GetSection("Stacks:Logging:SqlServer").Bind(options);
            this.Respond(options);
        }
    }
}