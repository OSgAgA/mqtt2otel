using System;
using System.Collections.Generic;
using System.Text;

namespace mqtt2otel.Configuration
{
    /// <summary>
    /// Provides the available payload types for open telemetry logging.
    /// </summary>
    public enum OtelLoggingPayloadType
    {
        /// <summary>
        /// Used for logging plain, unstructured text.
        /// </summary>
        Text = 0,

        /// <summary>
        /// Json format used for structured logs. 
        /// </summary>
        Json = 1,
    }
}
