using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Text;

namespace mqtt2otel.InternalMetrics
{
    /// <summary>
    /// Represents the meter for recording internal metrics regarding the otel coordinator.
    /// </summary>
    public class OtelMeter
    {
        /// <summary>
        /// The internally used meter.
        /// </summary>
        private Meter meter;

        /// <summary>
        /// Initializes a new instance of the <see cref="OtelMeter"/> class.
        /// </summary>
        public OtelMeter() 
        {
            this.meter = new Meter(nameof(OtelMeter));

            this.Connections = this.meter.CreateGauge<int>("mqtt2otel.otel.connections.sum", description: "This is the sum of all active connections to open telemetry endpoints.");
        }

        /// <summary>
        /// Gets the gauge for recording the sum of all active connections to open telemetry endpoints.
        /// </summary>
        public Gauge<int> Connections { get; }
    }
}
