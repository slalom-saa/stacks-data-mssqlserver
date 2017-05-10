using System.Linq;
using Serilog.Core;
using Serilog.Events;
using Slalom.Stacks.Runtime;
using Slalom.Stacks.Services.Messaging;

namespace Slalom.Stacks.Logging.SqlServer.Core
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
}