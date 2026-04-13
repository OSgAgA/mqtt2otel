using System;
using System.Collections.Generic;
using System.Text;

namespace mqtt2otel.Configuration
{
    /// <summary>
    /// Provides all supported open telemetry metric instrucments.
    /// 
    /// Synchronous instruments will be send directly to the otel endpoint, while
    /// asynchronous instruments will be actively collected by the endpoint.
    /// </summary>
    public enum OtelMetricInstrument
    {
        /// <summary>
        /// A synchronous gauge instrument.
        /// </summary>
        Gauge = 0,

        /// <summary>
        /// An asynchronous gauge instrument.
        /// </summary>
        AsynchronousGauge = 1,

        /// <summary>
        /// A synchronous counter instrument-
        /// </summary>
        Counter = 2,

        /// <summary>
        /// An asynchronous counter instrument.
        /// </summary>
        AsynchronousCounter = 3,

        /// <summary>
        /// A synchronous counter instrument.
        /// </summary>
        UpDownCounter = 4,

        /// <summary>
        /// An asynchronous UpDownCounter instrument.
        /// </summary>
        AsynchronousUpDownCounter = 5,

        /// <summary>
        /// A synchronous histogram.
        /// </summary>
        Histogram = 6
    }
}
