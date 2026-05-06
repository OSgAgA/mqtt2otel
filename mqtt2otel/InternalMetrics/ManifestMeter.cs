using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Text;

namespace mqtt2otel.InternalMetrics
{
    /// <summary>
    /// Represents a meter for recording manifest relevant internal metrics.
    /// </summary>
    public class ManifestMeter
    {
        /// <summary>
        /// The internally used meter.
        /// </summary>
        private Meter meter;

        /// <summary>
        /// Initializes a new instance of the <see cref="ManifestMeter"/> class.
        /// </summary>
        public ManifestMeter()
        {
            this.meter = new Meter(nameof(ManifestMeter));

            this.ManifestReadDuration = this.meter.CreateGauge<double>("mqtt2otel.manifest.processing_duration_in_us", unit: "us", "The time needed for reading, parsing, validating and initializing the manifest file.");
            this.ProcessorsCount = this.meter.CreateGauge<int>("mqtt2otel.manifest.procesors.count", "The amount of processors read from manifest file.");
            this.SubscriptionGroupsCount = this.meter.CreateGauge<int>("mqtt2otel.manifest.subscription_groups.count", "The amount of subscription groups read from the manifest file.");
            this.ManifestUnsuccessfulRead = this.meter.CreateCounter<int>("mqtt2otel.manifest.unsuccessful_read", "Counts the number of times a manifest could not be parsed successfully.");
        }

        /// <summary>
        /// Gets the gauge for recording the time needed for reading, parsing, validating and initializing the manifest file.
        /// </summary>
        public Gauge<double> ManifestReadDuration { get; }

        /// <summary>
        /// Gets gauge for recording the amount of processors read from manifest file.
        /// </summary>
        public Gauge<int> ProcessorsCount { get; }

        /// <summary>
        /// Gets the gauge for recording the amount of subscription groups read from the manifest file.
        /// </summary>
        public Gauge<int> SubscriptionGroupsCount { get; }

        /// <summary>
        /// Gets the counter for recording the number of times a manifest could not be parsed successfully.
        /// </summary>
        public Counter<int> ManifestUnsuccessfulRead { get; }
    }
}
