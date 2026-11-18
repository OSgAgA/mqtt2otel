using mqtt2otel.Manifest;
using mqtt2otel.Provider;
using mqtt2otel.Stores;

namespace mqtt2otel.Metadata
{
    /// <summary>
    /// Represents the internal state of the application.
    /// </summary>
    public class InternalState
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InternalState"/> class.
        /// </summary>
        public InternalState() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="InternalState"/> class.
        /// </summary>
        /// <param name="spans">The spans.</param>
        /// <param name="contexts">The span contexts.</param>

        public InternalState(List<OtelSpan> spans, List<OtelSpanContext> contexts)
        {
            this.Spans = spans;
            this.SpanContexts = contexts;
        }

        /// <summary>
        /// Gets or sets the internally created spans.
        /// </summary>
        public List<OtelSpan> Spans { get; set; } = new List<OtelSpan>();

        /// <summary>
        /// Gets or sets the internally created span contexts.
        /// </summary>
        public List<OtelSpanContext> SpanContexts { get; set; } = new List<OtelSpanContext>();

        /// <summary>
        /// Gets or sets the curren state of the trace id provider.
        /// </summary>
        public ProviderState? TraceIdProviderState { get; set; }

    }
}
