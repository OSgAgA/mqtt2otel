using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Text;

namespace mqtt2otel.InternalMetrics
{
    /// <summary>
    /// Represents a meter for recording internal metrics from the processors.
    /// </summary>
    public class ProcessorMeter
    {
        /// <summary>
        /// The internally used meter.
        /// </summary>
        private Meter meter;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessorMeter"/> class.
        /// </summary>
        public ProcessorMeter()
        {
            this.meter = new Meter(nameof(ProcessorMeter));

            this.ProcessingTimeTotal = this.meter.CreateHistogram<double>("mqtt2otel.processor.total.duration_in_us", unit: "us", description: "The total duration of processing all metric and otel rules of a single processor.");
            this.ProcessingTimeMetricRule = this.meter.CreateHistogram<double>("mqtt2otel.processor.metrics.rule.duration_in_us", unit: "us", description: "The duration of processing a single metrics rule inside a processor.");
            this.ProcessingTimeLoggingRule = this.meter.CreateHistogram<double>("mqtt2otel.processor.logging.rule.duration_in_us", unit: "us", description: "The duration of processing a single logging rule inside a processor.");
            this.ProcessingErrorCount = this.meter.CreateCounter<int>("mqtt2otel.processor.processing_errors", "A counter for measuring the amount of errors, that appeared while processing incoming data.");
            this.LogEntries = this.meter.CreateCounter<int>("mqtt2otel.processor.log_entries.count", description: "This is the count of created log entries.");
            this.Metrics = this.meter.CreateCounter<int>("mqtt2otel.processor.metrics.count", description: "This is the count of all created metrics.");
        }

        /// <summary>
        /// Gets the histogram to record the total duration of processing all metric and otel rules of a single processor.
        /// </summary>
        public Histogram<double> ProcessingTimeTotal { get; }

        /// <summary>
        /// Gets the histogram to record the duration of processing a single metrics rule inside a processor.
        /// </summary>
        public Histogram<double> ProcessingTimeMetricRule { get; }

        /// <summary>
        /// Gets the histogram to record the duration of processing a single logging rule inside a processor.
        /// </summary>
        public Histogram<double> ProcessingTimeLoggingRule { get; }

        /// <summary>
        /// Gets the counter to record the amount of errors, that appeared while processing incoming data.
        /// </summary>
        public Counter<int> ProcessingErrorCount { get; }

        /// <summary>
        /// Gets the counter to record the count of created log entries.
        /// </summary>
        public Counter<int> LogEntries { get; }

        /// <summary>
        /// Gets the counter to record all created metrics.
        /// </summary>
        public Counter<int> Metrics { get; }
    }
}
