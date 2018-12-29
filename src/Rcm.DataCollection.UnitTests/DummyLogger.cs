using System;
using Microsoft.Extensions.Logging;

namespace Rcm.DataCollection.UnitTests
{
    public class DummyLogger<T> : ILogger<T>
    {
        private class DummyScope : IDisposable
        {
            public void Dispose()
            {
            }
        }

        public IDisposable BeginScope<TState>(TState state) => new DummyScope();
        public bool IsEnabled(LogLevel logLevel) => false;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception exception,
            Func<TState, Exception, string> formatter)
        {
        }
    }
}