/* 
 * Copyright (c) Stacks Contributors
 * 
 * This file is subject to the terms and conditions defined in
 * the LICENSE file, which is part of this source code package.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using Serilog;
using Serilog.Context;
using Serilog.Core;
using Serilog.Sinks.MSSqlServer;
using Slalom.Stacks.Logging.SqlServer.Components.Locations;
using Slalom.Stacks.Logging.SqlServer.Settings;
using Slalom.Stacks.Validation;

namespace Slalom.Stacks.Logging.SqlServer.Components
{
    public class SqlLogger : ILogger
    {
        private readonly Logger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlLogger" /> class.
        /// </summary>
        /// <param name="options">The configured <see cref="SqlServerLoggingOptions" />.</param>
        /// <param name="policies">The configured <see cref="IDestructuringPolicy" /> instances.</param>
        /// <param name="locations">The configured <see cref="LocationStore" />.</param>
        public SqlLogger(SqlServerLoggingOptions options, IEnumerable<IDestructuringPolicy> policies, ILocationStore locations)
        {
            Argument.NotNull(options, nameof(options));

            var columnOptions = new ColumnOptions();

            // Don't include the Properties XML column.
            columnOptions.Store.Remove(StandardColumn.Properties);

            // Do include the log event data as JSON.
            columnOptions.Store.Add(StandardColumn.LogEvent);

            var builder = new LoggerConfiguration()
                .Destructure.With(policies.ToArray())
                .Enrich.FromLogContext()
                .WriteTo.StacksSqlServer(options.ConnectionString, options.TraceTableName, autoCreateSqlTable: true, columnOptions: columnOptions, locations: locations)
                .MinimumLevel.Is(options.GetLogLevel());

            _logger = builder.CreateLogger();
        }

        /// <summary>
        /// Write a log event with the debug level and associated exception.
        /// </summary>
        /// <param name="exception">Exception related to the event.</param>
        /// <param name="template">Message template describing the event.</param>
        /// <param name="properties">Objects positionally formatted into the message template.</param>
        public void Debug(Exception exception, string template, params object[] properties)
        {
            using (LogContext.PushProperties(new LogEventEnricher(properties)))
            {
                _logger.Debug(exception, template, properties);
            }
        }

        /// <summary>
        /// Write a log event with the debug level.
        /// </summary>
        /// <param name="template">Message template describing the event.</param>
        /// <param name="properties">Objects positionally formatted into the message template.</param>
        public void Debug(string template, params object[] properties)
        {
            using (LogContext.PushProperties(new LogEventEnricher(properties)))
            {
                _logger.Debug(template, properties);
            }
        }

        /// <summary>
        /// Write a log event with the error level and associated exception.
        /// </summary>
        /// <param name="exception">Exception related to the event.</param>
        /// <param name="template">Message template describing the event.</param>
        /// <param name="properties">Objects positionally formatted into the message template.</param>
        public void Error(Exception exception, string template, params object[] properties)
        {
            using (LogContext.PushProperties(new LogEventEnricher(properties)))
            {
                _logger.Error(exception, template, properties);
            }
        }

        /// <summary>
        /// Write a log event with the error level.
        /// </summary>
        /// <param name="template">Message template describing the event.</param>
        /// <param name="properties">Objects positionally formatted into the message template.</param>
        public void Error(string template, params object[] properties)
        {
            using (LogContext.PushProperties(new LogEventEnricher(properties)))
            {
                _logger.Error(template, properties);
            }
        }

        /// <summary>
        /// Write a log event with the fatal level and associated exception.
        /// </summary>
        /// <param name="exception">Exception related to the event.</param>
        /// <param name="template">Message template describing the event.</param>
        /// <param name="properties">Objects positionally formatted into the message template.</param>
        public void Fatal(Exception exception, string template, params object[] properties)
        {
            using (LogContext.PushProperties(new LogEventEnricher(properties)))
            {
                _logger.Fatal(exception, template, properties);
            }
        }

        /// <summary>
        /// Write a log event with the fatal level.
        /// </summary>
        /// <param name="template">Message template describing the event.</param>
        /// <param name="properties">Objects positionally formatted into the message template.</param>
        public void Fatal(string template, params object[] properties)
        {
            using (LogContext.PushProperties(new LogEventEnricher(properties)))
            {
                _logger.Fatal(template, properties);
            }
        }

        /// <summary>
        /// Write a log event with the information level and associated exception.
        /// </summary>
        /// <param name="exception">Exception related to the event.</param>
        /// <param name="template">Message template describing the event.</param>
        /// <param name="properties">Objects positionally formatted into the message template.</param>
        public void Information(Exception exception, string template, params object[] properties)
        {
            using (LogContext.PushProperties(new LogEventEnricher(properties)))
            {
                _logger.Information(exception, template, properties);
            }
        }

        /// <summary>
        /// Write a log event with the information level.
        /// </summary>
        /// <param name="template">Message template describing the event.</param>
        /// <param name="properties">Objects positionally formatted into the message template.</param>
        public void Information(string template, params object[] properties)
        {
            using (LogContext.PushProperties(new LogEventEnricher(properties)))
            {
                _logger.Information(template, properties);
            }
        }

        /// <summary>
        /// Write a log event with the verbose level and associated exception.
        /// </summary>
        /// <param name="exception">Exception related to the event.</param>
        /// <param name="template">Message template describing the event.</param>
        /// <param name="properties">Objects positionally formatted into the message template.</param>
        public void Verbose(Exception exception, string template, params object[] properties)
        {
            using (LogContext.PushProperties(new LogEventEnricher(properties)))
            {
                _logger.Verbose(exception, template, properties);
            }
        }

        /// <summary>
        /// Write a log event with the verbose level.
        /// </summary>
        /// <param name="template">Message template describing the event.</param>
        /// <param name="properties">Objects positionally formatted into the message template.</param>
        public void Verbose(string template, params object[] properties)
        {
            using (LogContext.PushProperties(new LogEventEnricher(properties)))
            {
                _logger.Verbose(template, properties);
            }
        }

        /// <summary>
        /// Write a log event with the warning level and associated exception.
        /// </summary>
        /// <param name="exception">Exception related to the event.</param>
        /// <param name="template">Message template describing the event.</param>
        /// <param name="properties">Objects positionally formatted into the message template.</param>
        public void Warning(Exception exception, string template, params object[] properties)
        {
            using (LogContext.PushProperties(new LogEventEnricher(properties)))
            {
                _logger.Warning(exception, template, properties);
            }
        }

        /// <summary>
        /// Write a log event with the warning level.
        /// </summary>
        /// <param name="template">Message template describing the event.</param>
        /// <param name="properties">Objects positionally formatted into the message template.</param>
        public void Warning(string template, params object[] properties)
        {
            using (LogContext.PushProperties(new LogEventEnricher(properties)))
            {
                _logger.Warning(template, properties);
            }
        }

        #region IDisposable Implementation

        private bool _disposed;

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="SerilogLogger" /> class.
        /// </summary>
        ~SqlLogger()
        {
            this.Dispose(false);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                // free other managed objects that implement IDisposable only
                _logger.Dispose();
            }

            // release any unmanaged objects
            // set the object references to null

            _disposed = true;
        }

        #endregion
    }
}