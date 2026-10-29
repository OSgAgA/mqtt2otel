using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Runtime.InteropServices;
using System.Text;

namespace mqtt2otel.InternalMetrics
{
    /// <summary>
    /// Represents a meter for recording manifest relevant internal metrics.
    /// </summary>
    public class UsageTelemetryMeter
    {
        /// <summary>
        /// The internally used meter.
        /// </summary>
        private Meter meter;

        /// <summary>
        /// Initializes a new instance of the <see cref="ManifestMeter"/> class.
        /// </summary>
        public UsageTelemetryMeter()
        {
            this.meter = new Meter(nameof(UsageTelemetryMeter), "1.0.0");

            this.ManifestOtelConnections = this.meter.CreateGauge<long>("mqtt2otel.telemetry.manifest.otel-connections", description: "The count of the available otel connections in the manifest file.");
            this.ManifestMqttConnections = this.meter.CreateGauge<long>("mqtt2otel.telemetry.manifest.mqtt-connections", description: "The count of the available otel connections in the manifest file.");
            this.ProcessorsCount = this.meter.CreateGauge<long>("mqtt2otel.telemetry.manifest.procesors.count", description: "The amount of processors read from manifest file.");
            this.LogProcessorsCount = this.meter.CreateGauge<long>("mqtt2otel.telemetry.manifest.log-procesors.count", description: "The amount of log processors read from manifest file.");
            this.MetricProcessorsCount = this.meter.CreateGauge<long>("mqtt2otel.telemetry.manifest.metric-procesors.count", description: "The amount of metric processors read from manifest file.");
            this.MqttSubscriptions = this.meter.CreateGauge<long>("mqtt2otel.telemetry.manifest.mqtt-subscriptions", description: "The amount of mqtt topic subscriptions.");

            this.AvailableMemoryInBytes = this.meter.CreateGauge<long>("mqtt2otel.telemetry.system.available_memory_in_bytes", description: "The amount of available memory in bytes.", unit: "By");
            this.MemoryConsumptionInByte = this.meter.CreateObservableGauge<long>("mqtt2otel.telemetry.system.memory_consumption_in_bytes", () => GC.GetTotalMemory(forceFullCollection: false),
                description: "Gets the memory consumption of the appliaction in bytes.", unit: "By");
            this.MemoryConumptionInPercentage = this.meter.CreateObservableGauge<long>("mqtt2otel.telemetry.system.memory_consumption_in_percentage",
                () => 1 / GC.GetGCMemoryInfo().TotalAvailableMemoryBytes * GC.GetTotalMemory(forceFullCollection: false),
                description: "Gets the total memory consumption of the application in percentage relative to the total available memory.", unit: "%");
            
            this.MessageCount = this.meter.CreateCounter<long>("mqtt2otel.telemetry.messages.count", description: "The amount of messages received (either processed or failed or ignored.");
            this.LogProcessingTime = this.meter.CreateHistogram<double>("mqtt2otel.telemetry.message.log-processing", unit: "us", description: "The histogram of the log processing time.");
            this.MetricProcessingTime = this.meter.CreateHistogram<double>("mqtt2otel.telemetry.message.metric-processing", unit: "us", description: "The histogram of the metric processing time.");
        }

        /// <summary>
        /// Gets the count of the available otel connections in the manifest file.
        /// </summary>
        public Gauge<long> ManifestOtelConnections { get; }

        /// <summary>
        /// Gets the count of the available otel connections in the manifest file.
        /// </summary>
        public Gauge<long> ManifestMqttConnections { get; }

        /// <summary>
        /// Gets the amount of processors read from manifest file..
        /// </summary>
        public Gauge<long> ProcessorsCount { get; }

        /// <summary>
        /// Gets the amount of log processors read from manifest file.
        /// </summary>
        public Gauge<long> LogProcessorsCount { get; }

        /// <summary>
        /// Gets the amount of metric processors read from manifest file.
        /// </summary>
        public Gauge<long> AvailableMemoryInBytes { get; }

        /// <summary>
        /// Gets the amount of metric processors read from manifest file.
        /// </summary>
        public Gauge<long> MetricProcessorsCount { get; }

        /// <summary>
        /// Gets the amount of mqtt topic subscriptions.
        /// </summary>
        public Gauge<long> MqttSubscriptions { get; }

        /// <summary>
        /// Gets the amount of messages received (either processed or failed or ignored.
        /// </summary>
        public Counter<long> MessageCount { get; }

        /// <summary>
        /// Gets the histogram of the log processing time.
        /// </summary>
        public Histogram<double> LogProcessingTime { get; }

        /// <summary>
        /// Gets the histogram of the metric processing time.
        /// </summary>
        public Histogram<double> MetricProcessingTime { get; }

        /// <summary>
        /// Gets the total memory consumption of the application in bytes.
        /// </summary>
        public ObservableGauge<long> MemoryConsumptionInByte { get; }

        /// <summary>
        /// Gets the total memory consumption of the application in percentage relative to the total available memory.
        /// </summary>
        public ObservableGauge<long> MemoryConumptionInPercentage { get; }
    }
}
