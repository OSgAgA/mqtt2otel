using Microsoft.Extensions.Logging;
using mqtt2otel.Manifest;
using System;
using System.Collections.Generic;
using System.Text;

namespace mqtt2otel.InternalMetrics
{
    /// <summary>
    /// Represents the application settings for internal mqtt2otel metrics.
    /// </summary>
    public class InternalMetricsSettings
    {
        /// <summary>
        /// Gets or sets a value indicating, whether the application should collect metrics and send them to an open telemetry endpoint.
        /// </summary>
        public bool CollectMetrics { get; set; } = false;

        /// <summary>
        /// Gets or sets the otel server settings. Will be ignored if <see cref="LogToOtel"/> is false.
        /// </summary>
        public OtelServerConnection? Otel { get; set; } = null;

    }
}
