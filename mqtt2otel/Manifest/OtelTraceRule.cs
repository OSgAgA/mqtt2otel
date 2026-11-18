using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace mqtt2otel.Manifest
{
    /// <summary>
    /// Represents a rule for creating traces and spans.
    /// </summary>
    public class OtelTraceRule : Rule
    {
        /// <summary>
        /// Gets or sets information used for creating and starting new spans.
        /// </summary>
        public ImportEnabledList<OtelSpan> CreateSpans { get; set; } = new();

        /// <summary>
        /// Gets or sets the information used for ending existing spans.
        /// </summary>
        public ImportEnabledList<OtelSpan> EndSpans { get; set; } = new();

        /// <summary>
        /// Gets or sets the events that should be added to a span.
        /// </summary>
        public List<OtelSpanEvent> CreateEvents { get; set; } = new();

        /// <summary>
        /// Gets or sets the timespan for how long a span will be kept alive.
        /// </summary>
        [InheritedProperty]
        public int? RetentionInSeconds { get; set; } = null;

        /// <summary>
        /// Gets or sets the <see cref="SpanRetentionStrategy"/> used, when the retention time is exceeded.
        /// </summary>
        [InheritedProperty]
        public SpanRetentionStrategy? RetentionStrategy { get; set; } = null;

        /// <summary>
        /// Gets or sets the event that will be created, when the retention time is exceeded. Only applies if a strategy is chosen, 
        /// that creates events.
        /// </summary>
        [InheritedProperty]
        public OtelSpanEvent? RetentionExceededEvent { get; set; } = null;

        /// <summary>
        /// Gets or sets the time interval in seconds that is waited for late span events, before a span is send after it is completed.
        /// </summary>
        [InheritedProperty]
        public int? WaitForLateSpanEventsInS { get; set; } = null;
    }
}
