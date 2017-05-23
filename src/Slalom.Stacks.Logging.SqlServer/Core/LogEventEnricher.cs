/* 
 * Copyright (c) Stacks Contributors
 * 
 * This file is subject to the terms and conditions defined in
 * the LICENSE file, which is part of this source code package.
 */

using System.Linq;
using System.Reflection;
using Serilog.Core;
using Serilog.Events;
using Slalom.Stacks.Configuration;
using Slalom.Stacks.Services.Messaging;

namespace Slalom.Stacks.Logging.SqlServer.Core
{
    public class LogEventEnricher : ILogEventEnricher
    {
        private readonly ExecutionContext _context;
        private Application _environment;

        public LogEventEnricher(object[] context)
        {
            _context = context.OfType<ExecutionContext>().FirstOrDefault();
            _environment = context.OfType<Application>().FirstOrDefault();
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
                logEvent.AddPropertyIfAbsent(new LogEventProperty("ApplicationName", new ScalarValue(_environment.Title)));
                logEvent.AddPropertyIfAbsent(new LogEventProperty("EnvironmentName", new ScalarValue(_environment.Environment)));
                logEvent.AddPropertyIfAbsent(new LogEventProperty("MachineName", new ScalarValue(System.Environment.MachineName)));
                logEvent.AddPropertyIfAbsent(new LogEventProperty("Build", new ScalarValue(Assembly.GetEntryAssembly().GetName().Version.ToString())));
                logEvent.AddPropertyIfAbsent(new LogEventProperty("Version", new ScalarValue(_environment.Version)));
            }
        }
    }
}