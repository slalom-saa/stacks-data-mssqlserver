/*
 * Copyright 2017 Stacks Contributors
 * 
 * This file is subject to the terms and conditions defined in
 * file 'LICENSE.txt', which is part of this source code package.
 */

using System;
using System.Collections.Generic;
using Autofac;
using Serilog.Core;
using Slalom.Stacks.Configuration;
using Slalom.Stacks.Runtime;
using Slalom.Stacks.Validation;

namespace Slalom.Stacks.Logging.SqlServer
{
    /// <summary>
    /// An Autofac module for the SQL Server Logging module.
    /// </summary>
    /// <seealso cref="Autofac.Module" />
    public class SqlServerLoggingModule : Module
    {
        private readonly SqlServerLoggingOptions _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlServerLoggingModule"/> class.
        /// </summary>
        /// <param name="options">The options to use.</param>
        public SqlServerLoggingModule(SqlServerLoggingOptions options)
        {
            Argument.NotNull(options, nameof(options));

            _options = options;
        }

        /// <summary>
        /// Override to add registrations to the container.
        /// </summary>
        /// <param name="builder">The builder through which components can be
        /// registered.</param>
        /// <remarks>Note that the ContainerBuilder parameter is unique to this module.</remarks>
        protected override void Load(ContainerBuilder builder)
        {
            base.Load(builder);

            builder.Register(c => new SqlConnectionManager(_options.ConnectionString))
                   .SingleInstance();

            builder.Register(c => new DestructuringPolicy()).SingleInstance()
                   .AsImplementedInterfaces();

            builder.Register(c => new SqlLogger(_options, c.Resolve<IEnumerable<IDestructuringPolicy>>(), c.Resolve<LocationStore>()))
                   .AsImplementedInterfaces()
                   .SingleInstance()
                   .PreserveExistingDefaults();

            builder.Register(c => new ResponseLog(_options, c.Resolve<SqlConnectionManager>(), c.Resolve<LocationStore>()))
                   .AsImplementedInterfaces()
                   .AsSelf()
                   .SingleInstance()
                   .PropertiesAutowired(AllProperties.Instance)
                   .OnActivated(c =>
                   {
                       var table = new SqlTableCreator(_options.ConnectionString);
                       table.CreateTable(c.Instance.CreateTable());
                   });

            builder.Register(c => new RequestLog(_options, c.Resolve<SqlConnectionManager>(), c.Resolve<LocationStore>()))
                   .AsImplementedInterfaces()
                   .AsSelf()
                   .SingleInstance()
                   .PropertiesAutowired(AllProperties.Instance)
                   .OnActivated(c =>
                   {
                       var table = new SqlTableCreator(_options.ConnectionString);
                       table.CreateTable(c.Instance.CreateTable());
                   });

            builder.Register(c => new IPInformationProvider());

            builder.Register(c => new LocationStore(c.Resolve<SqlConnectionManager>(), c.Resolve<IPInformationProvider>()))
                   .AsImplementedInterfaces()
                   .AsSelf()
                   .SingleInstance()
                   .PropertiesAutowired(AllProperties.Instance)
                   .OnActivated(c =>
                   {
                       var table = new SqlTableCreator(_options.ConnectionString);
                       table.CreateTable(c.Instance.CreateTable());
                   });
        }
    }
}