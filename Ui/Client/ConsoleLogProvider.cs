using System;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Ui.Client
{
    public class ConsoleLogProvider : ILoggerProvider
    {
        public void Dispose()
        {
            throw new System.NotImplementedException();
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new ConsoleLogger();
        }

        public class ConsoleLogger : ILogger
        {
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception,
                Func<TState, Exception, string> formatter)
            {
                Console.WriteLine(JsonSerializer.Serialize(new
                {
                    logLevel,
                    exception,
                    state
                }));
            }

            public bool IsEnabled(LogLevel logLevel)
            {
                return true;
            }

            public IDisposable BeginScope<TState>(TState state)
            {
                return null;
            }
        }
    }
}