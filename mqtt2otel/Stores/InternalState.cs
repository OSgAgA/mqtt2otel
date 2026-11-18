using mqtt2otel.Manifest;
using mqtt2otel.Stores;

namespace mqtt2otel.Stores
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

        public InternalState(IEnumerable<OtelSpan> spans, IEnumerable<OtelSpanContext> contexts)
        {
            this.Spans = spans;
            this.SpanContexts = contexts;
        }

        /// <summary>
        /// Gets or sets the internally created spans.
        /// </summary>
        public IEnumerable<OtelSpan> Spans { get; set; } = new List<OtelSpan>();

        /// <summary>
        /// Gets or sets the internally created span contexts.
        /// </summary>
        public IEnumerable<OtelSpanContext> SpanContexts { get; set; } = new List<OtelSpanContext>();
    }
}
