using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace mqtt2otel.InternalLogging
{
    /// <summary>
    /// A helper class with static logging extensions
    /// </summary>
    public static class ILoggerExtensions
    {
        /// <summary>
        /// Starts an activity on the <see cref="InternalLogFactory.MainActivitySource"/>.
        /// </summary>
        /// <param name="logger">The logger on which this extension is called.</param>
        /// <param name="name">The activity name.</param>
        /// <returns>An <see cref="IDisposable"/> object.</returns>
        public static IDisposable? StartActivity (this ILogger logger, string name)
        {
            return InternalLogFactory.MainActivitySource.StartActivity(name);
        }
    }
}
