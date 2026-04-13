using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace mqtt2otel.InternalLogging
{
    /// <summary>
    /// Represents an empty default logger.
    /// </summary>
    /// <typeparam name="T">The type of the logger.</typeparam>
    public class EmptyLogger<T> : ILogger<T>
    {
        /// <inheritdoc/>
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            return null;
        }

        /// <inheritdoc/>
        public bool IsEnabled(LogLevel logLevel)
        {
            return false;
        }

        /// <inheritdoc/>
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            return;
        }
    }
}
