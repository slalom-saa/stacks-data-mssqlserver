﻿/* 
 * Copyright (c) Stacks Contributors
 * 
 * This file is subject to the terms and conditions defined in
 * the LICENSE file, which is part of this source code package.
 */

using System;
using System.Collections;
using Autofac;
using Microsoft.Extensions.Configuration;
using Slalom.Stacks.Logging.SqlServer.Module;
using Slalom.Stacks.Logging.SqlServer.Settings;
using Slalom.Stacks.Validation;

namespace Slalom.Stacks.Logging.SqlServer
{
    /// <summary>
    /// Contains extension methods to add SQL Server Logging blocks.
    /// </summary>
    public static class Configuration
    {
        /// <summary>
        /// Adds the SQL Server Auditing block to the container.
        /// </summary>
        /// <param name="instance">The this instance.</param>
        /// <param name="configuration">The configuration routine.</param>
        /// <returns>Returns the container instance for method chaining.</returns>
        public static Stack UseSqlServerLogging(this Stack instance, Action<SqlServerLoggingOptions> configuration = null)
        {
            Argument.NotNull(instance, nameof(instance));

            instance.Include(typeof(Configuration).Assembly);

            var options = new SqlServerLoggingOptions();
            configuration?.Invoke(options);
            instance.Configuration.GetSection("Stacks:Logging:SqlServer").Bind(options);

            instance.Use(builder =>
            {
                builder.RegisterModule(new SqlServerLoggingModule(options));
            });

            return instance;
        }
    }
}