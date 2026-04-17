using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace mqtt2otel.InternalLogging
{
    /// <summary>
    /// A wrapper class that represents a logger that is listening to an activity source and is able to format the 
    /// log output accordingly.
    /// </summary>
    public class ActivityLoggerWrapper : ILogger
    {
        /// <summary>
        /// The wrapped logger.
        /// </summary>
        private readonly ILogger innerLogger;

        /// <summary>
        /// The activity source, that the logger is listing to.
        /// </summary>
        private readonly ActivitySource activitySource;

        /// <summary>
        /// A counter, that counts the levels of activities inside the activity hierarchy. Used for indenting the
        /// log ouput.
        /// </summary>
        private int indentCounter = 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="ActivityLoggerWrapper"/> class.
        /// </summary>
        /// <param name="innerLogger">The wrapped logger.</param>
        /// <param name="source">The activity source to listen to.</param>
        public ActivityLoggerWrapper(ILogger innerLogger, ActivitySource source) 
        {
            this.innerLogger = innerLogger;
            this.activitySource = source;

            var listener = new ActivityListener
            {
                ShouldListenTo = source => source.Name == source.Name,
                Sample = (ref ActivityCreationOptions<ActivityContext> options) =>
                    ActivitySamplingResult.AllDataAndRecorded,
                ActivityStarted = activity =>
                {
                    this.indentCounter += 1;
                },
                ActivityStopped = activity =>
                {
                    this.indentCounter -= 1;
                }
            };

            ActivitySource.AddActivityListener(listener);
        }

        /// <inheritdoc/>
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            return this.innerLogger.BeginScope(state);
        }

        /// <inheritcdoc/>
        public bool IsEnabled(LogLevel logLevel)
        {
            return this.innerLogger.IsEnabled(logLevel);
        }

        /// <inheritdoc/>
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var message = formatter(state, exception);

            if (this.indentCounter < 0) this.indentCounter = 0;

            var indented = new string(' ', this.indentCounter * 4) + message;

            this.innerLogger.Log(
                logLevel,
                eventId,
                indented,
                exception,
                (s, e) => s
            );
        }

    }
}
