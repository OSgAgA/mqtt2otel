using System.Diagnostics.Metrics;

namespace mqtt2otel.ManifestExplorer.Meters
{
    /// <summary>
    /// Tracks the usage parameters for the manifest explorer.
    /// </summary>
    public class UsageMeter
    {
        /// <summary>
        /// The name prefix for all metrics.
        /// </summary>
        private const string prefix = "mqtt2otel.manifest_explorer.";

        /// <summary>
        /// The internally used meter.
        /// </summary>
        private Meter meter;

        /// <summary>
        /// Initializes a new instance of the <see cref="UsageMeter"/> class.
        /// </summary>
        public UsageMeter()
        {
            this.meter = new Meter(nameof(UsageMeter));

            this.PageRequestedCounter = this.meter.CreateCounter<long>(prefix + "page_requested_counter", description: "Counts how often a page is requested from a user.");
            this.ExampleRequestedCounter = this.meter.CreateCounter<long>(prefix + "example_requested_counter", description: "Counts how often an example code is requested from a user.");
            this.ExampleAppliedCounter = this.meter.CreateCounter<long>(prefix + "example_applied_counter", description: "Counts how often the apply button is clicked from a user.");
        }

        /// <summary>
        /// Gets a counter that counts how often a page is requested from a user.
        /// </summary>
        public Counter<long> PageRequestedCounter { get; }

        /// <summary>
        /// Gets a counter that counts how often an example code is requested from a user.
        /// </summary>
        public Counter<long> ExampleRequestedCounter { get; }

        /// <summary>
        /// Gets a counter that counts how often the apply button is clicked from a user.
        /// </summary>
        public Counter<long> ExampleAppliedCounter { get; }
    }
}
