using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Text;

namespace mqtt2otel.InternalMetrics
{
    /// <summary>
    /// Represents a meter for recording mqtt relevant internal metrics.
    /// </summary>
    public class MqttMeter 
    {
        /// <summary>
        /// The internally used meter.
        /// </summary>
        Meter meter;

        /// <summary>
        /// Initializes a new instance of the <see cref="MqttMeter"/> class.
        /// </summary>
        public MqttMeter()
        {
            this.meter = new Meter(nameof(MqttMeter));

            this.SubscriptionsCount = this.meter.CreateGauge<int>("mqtt2otel.mqtt.subscriptions.count", description: "This is the sum of all subscriptions. A wildcard subscription counts as one subscription.");
            this.MessagesReceived = this.meter.CreateCounter<int>("mqtt2otel.mqtt.messages.received", description: "The number of mqtt messages received.");
            this.ConnectionCount = this.meter.CreateGauge<int>("mqtt2otel.mqtt.connection.count", description: "The total amount of mqtt broker connections.");
            this.PayloadSize = this.meter.CreateHistogram<long>("mqtt2otel.mqtt.payload_size_in_bytes", unit: "b", description: "The size of the mqtt payloads in bytes.");
        }

        /// <summary>
        /// Gets the gauge for recording the sum of all subscriptions. A wildcard subscription counts as one.
        /// </summary>
        public Gauge<int> SubscriptionsCount { get; private set; }

        /// <summary>
        /// Gets the gauge for recording the number of mqtt messages received.
        /// </summary>
        public Counter<int> MessagesReceived { get; }

        /// <summary>
        /// Gets the gauge for recording the total amount of mqtt broker connections.
        /// </summary>
        public Gauge<int> ConnectionCount { get; }

        /// <summary>
        /// Gets the histogram for recording the size of the mqtt payloads in bytes.
        /// </summary>
        public Histogram<long> PayloadSize { get; }
    }
}
