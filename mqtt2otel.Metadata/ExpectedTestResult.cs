using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using System;
using System.Collections.Generic;
using System.Text;

namespace mqtt2otel.Metadata
{
    /// <summary>
    /// Represents an expected result of a system test.
    /// </summary>
    public class ExpectedTestResult
    {
        /// <summary>
        /// Gets or sets the expectation for metrics.
        /// </summary>
        public List<MetricTestData> Metrics { get; set; } = new();

        /// <summary>
        /// Gets or sets the expectation for logs.
        /// </summary>
        public List<LogTestData> Logs { get; set; } = new();

        /// <summary>
        /// Gets or sets the expectation for spans.
        /// </summary>
        public List<SpanTestData> Spans { get; set; } = new();

        /// <summary>
        /// Gets or sets the expectation for the internal state.
        /// </summary>
        public InternalState State { get; set; } = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="ExpectedTestResult"/> class.
        /// 
        /// This constructor is for serialization only, and should not be used directly.
        /// </summary>
        public ExpectedTestResult() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExpectedTestResult"/> class.
        /// </summary>
        /// <param name="metrics">The expected metric results.</param>
        /// <param name="logs">The expected log results.</param>
        /// <param name="spans">The expected span results.</param>
        /// <param name="state">The internal state.</param>
        public ExpectedTestResult(List<MetricTestData> metrics, List<LogTestData> logs, List<SpanTestData> spans, InternalState state)
        {
            this.Metrics = metrics;
            this.Logs = logs;
            this.Spans = spans;
            this.State = state;
        }
    }
}