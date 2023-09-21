using System;
using Microsoft.Extensions.Logging;

namespace Rcm.Common.TestDoubles
{
    // TODO Replace with NullLogger<T>.Instance (Zdenek Jelinek, 21. 9. 2023)
    [Obsolete("Use NullLogger<T>.Instance")]
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
            Exception? exception,
            Func<TState, Exception, string> formatter)
        {
        }
    }
}