using System;
using System.Collections.Generic;
using System.Linq;
using Serilog;
using Serilog.Core;
using Serilog.Sinks.MSSqlServer;
using Slalom.Stacks.Runtime;
using Slalom.Stacks.Validation;
using Serilog.Context;
using Serilog.Events;
using Slalom.Stacks.Services.Messaging;
using Environment = Slalom.Stacks.Runtime.Environment;

namespace Slalom.Stacks.Logging.SqlServer
{
    public class LogEventEnricher : ILogEventEnricher
    {
        private readonly ExecutionContext _context;
        private Environment _environment;

        public LogEventEnricher(object[] context)
        {
            _context = context.OfType<ExecutionContext>().FirstOrDefault();
            _environment = context.OfType<Environment>().FirstOrDefault();
        }

        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            if (_context != null)
            {
                if (_context.Request.User != null && _context.Request.User.Identity.IsAuthenticated)
                {
                    logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("UserName", _context.Request.User.Identity.Name));
                }
                logEvent.AddPropertyIfAbsent(new LogEventProperty("CorrelationId", new ScalarValue(_context.Request.CorrelationId)));
                logEvent.AddPropertyIfAbsent(new LogEventProperty("SourceAddress", new ScalarValue(_context.Request.SourceAddress)));
                logEvent.AddPropertyIfAbsent(new LogEventProperty("SessionId", new ScalarValue(_context.Request.SessionId)));
            }
            if (_environment != null)
            {
                logEvent.AddPropertyIfAbsent(new LogEventProperty("ApplicationName", new ScalarValue(_environment.ApplicationName)));
                logEvent.AddPropertyIfAbsent(new LogEventProperty("EnvironmentName", new ScalarValue(_environment.EnvironmentName)));
                logEvent.AddPropertyIfAbsent(new LogEventProperty("MachineName", new ScalarValue(_environment.MachineName)));
                logEvent.AddPropertyIfAbsent(new LogEventProperty("ThreadId", new ScalarValue(_environment.ThreadId)));
            }
        }
    }

    public class SqlLogger : ILogger
    {
        private readonly Logger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlLogger" /> class.
        /// </summary>
        /// <param name="options">The configured <see cref="SqlServerLoggingOptions" />.</param>
        /// <param name="policies">The configured <see cref="IDestructuringPolicy"/> instances.</param>
        /// <param name="locations">The configured <see cref="LocationStore" />.</param>
        public SqlLogger(SqlServerLoggingOptions options, IEnumerable<IDestructuringPolicy> policies, LocationStore locations)
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
                .MinimumLevel.Is(options.TraceLogLevel);

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
            _logger.Debug(exception, template, properties);
        }

        /// <summary>
        /// Write a log event with the debug level.
        /// </summary>
        /// <param name="template">Message template describing the event.</param>
        /// <param name="properties">Objects positionally formatted into the message template.</param>
        public void Debug(string template, params object[] properties)
        {
            _logger.Debug(template, properties);
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
            _logger.Error(template, properties);
        }

        /// <summary>
        /// Write a log event with the fatal level and associated exception.
        /// </summary>
        /// <param name="exception">Exception related to the event.</param>
        /// <param name="template">Message template describing the event.</param>
        /// <param name="properties">Objects positionally formatted into the message template.</param>
        public void Fatal(Exception exception, string template, params object[] properties)
        {
            _logger.Fatal(exception, template, properties);
        }

        /// <summary>
        /// Write a log event with the fatal level.
        /// </summary>
        /// <param name="template">Message template describing the event.</param>
        /// <param name="properties">Objects positionally formatted into the message template.</param>
        public void Fatal(string template, params object[] properties)
        {
            _logger.Fatal(template, properties);
        }

        /// <summary>
        /// Write a log event with the information level and associated exception.
        /// </summary>
        /// <param name="exception">Exception related to the event.</param>
        /// <param name="template">Message template describing the event.</param>
        /// <param name="properties">Objects positionally formatted into the message template.</param>
        public void Information(Exception exception, string template, params object[] properties)
        {
            _logger.Information(exception, template, properties);
        }

        /// <summary>
        /// Write a log event with the information level.
        /// </summary>
        /// <param name="template">Message template describing the event.</param>
        /// <param name="properties">Objects positionally formatted into the message template.</param>
        public void Information(string template, params object[] properties)
        {
            _logger.Information(template, properties);
        }

        /// <summary>
        /// Write a log event with the verbose level and associated exception.
        /// </summary>
        /// <param name="exception">Exception related to the event.</param>
        /// <param name="template">Message template describing the event.</param>
        /// <param name="properties">Objects positionally formatted into the message template.</param>
        public void Verbose(Exception exception, string template, params object[] properties)
        {
            _logger.Verbose(exception, template, properties);
        }

        /// <summary>
        /// Write a log event with the verbose level.
        /// </summary>
        /// <param name="template">Message template describing the event.</param>
        /// <param name="properties">Objects positionally formatted into the message template.</param>
        public void Verbose(string template, params object[] properties)
        {
            _logger.Verbose(template, properties);
        }

        /// <summary>
        /// Write a log event with the warning level and associated exception.
        /// </summary>
        /// <param name="exception">Exception related to the event.</param>
        /// <param name="template">Message template describing the event.</param>
        /// <param name="properties">Objects positionally formatted into the message template.</param>
        public void Warning(Exception exception, string template, params object[] properties)
        {
            _logger.Warning(exception, template, properties);
        }

        /// <summary>
        /// Write a log event with the warning level.
        /// </summary>
        /// <param name="template">Message template describing the event.</param>
        /// <param name="properties">Objects positionally formatted into the message template.</param>
        public void Warning(string template, params object[] properties)
        {
            _logger.Warning(template, properties);
        }

        #region IDisposable Implementation

        bool _disposed;

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="SerilogLogger"/> class.
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