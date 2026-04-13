using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace mqtt2otel.InternalLogging
{
    /// <summary>
    /// Represents a logger provider wrapper, that will be able to create loggers that will listen to an activity source to 
    /// create traces.
    /// </summary>
    public class ActivityLoggerProvider : ILoggerProvider
    {
        /// <summary>
        /// The inner logger provider that will be called internally.
        /// </summary>
        private readonly ILoggerProvider innerLoggerProvider;

        /// <summary>
        /// The activity source, the logger is bound to.
        /// </summary>
        private readonly ActivitySource activitySource;

        /// <summary>
        /// Initializes a new instance of the <see cref="ActivityLoggerProvider"/> class.
        /// </summary>
        /// <param name="loggerProvider">The wrapped logger provider.</param>
        /// <param name="activitySource">The activity source, that the loggers will listen to.</param>
        public ActivityLoggerProvider(ILoggerProvider loggerProvider, ActivitySource activitySource) 
        { 
            this.innerLoggerProvider = loggerProvider;
            this.activitySource = activitySource;
        }

        /// <summary>
        /// Creates a new logger with the given category name.
        /// </summary>
        /// <param name="categoryName">The logger category name.</param>
        /// <returns>The created logger.</returns>
        public ILogger CreateLogger(string categoryName)
        {
            var logger = this.innerLoggerProvider.CreateLogger(categoryName);
            return new ActivityLoggerWrapper(logger, this.activitySource);
        }

        /// <summary>
        /// Disposes the object.
        /// </summary>
        public void Dispose()
        {
            this.innerLoggerProvider?.Dispose();
        }
    }
}
