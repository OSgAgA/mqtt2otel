using System;
using System.Collections.Generic;
using System.Text;

namespace mqtt2otel
{
    /// <summary>
    /// Represents the available subscription types.
    /// </summary>
    public enum SubscriptionType
    {
        /// <summary>
        /// A subscription that provides payloads for creating open telemetry metrics.
        /// </summary>
        Metric,

        /// <summary>
        /// A subscription that provides payloads for creating open telemetry logs.
        /// </summary>
        Log
    }
}
