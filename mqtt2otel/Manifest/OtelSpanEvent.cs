using mqtt2otel.Helper;
using mqtt2otel.Parser;
using System;
using System.Collections.Generic;
using System.Text;

namespace mqtt2otel.Manifest
{
    /// <summary>
    /// Represents an open telemetry span event.
    /// </summary>
    public class OtelSpanEvent
    {
        /// <summary>
        /// Gets or sets the name of the event.
        /// </summary>
        public string Name { get; set; } = "Event";

        /// <summary>
        /// Gets or sets the span id of the event.
        /// </summary>
        public string SpanId { get; set; } = string.Empty;

        /// <summary>
        /// Backing field for <see cref="Completed"/> property.
        /// </summary>
        private string? timestamp = null;

        /// <summary>
        /// Gets or sets the timestamp, when the event happened. Defaults to DateTime.Now.
        /// </summary>
        public string? Timestamp
        {
            get => timestamp;
            set
            {
                timestamp = value;

                if (value == null) this.TimestampAsDateTime = DateTime.UtcNow;

                if (DateTime.TryParse(value, out var result))
                {
                    this.TimestampAsDateTime = result;
                }

                this.TimestampAsDateTime = TimestampAsDateTime;
            }
        }

        /// <summary>
        /// Gets the timestamp as a date time value or null, if no valid timestamp exists.
        /// </summary>
        public DateTime TimestampAsDateTime { get; internal set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the attributes associated with the event.
        /// </summary>
        public List<OtelAttribute> Attributes { get; set; } = new List<OtelAttribute>();

        /// <summary>
        /// Clones the object.
        /// </summary>
        /// <returns>The created clone.</returns>
        public OtelSpanEvent Clone()
        {
            var clone = (OtelSpanEvent)MemberwiseClone();

            clone.Attributes = new List<OtelAttribute>();
            foreach (var attribute in this.Attributes)
            {
                clone.Attributes.Add(attribute.Clone());
            }

            return clone;
        }

        /// <summary>
        /// Expands all values by applying the provided parser.
        /// </summary>
        /// <param name="embeddedExpressionParser">The embedded expression parser.</param>
        /// <param name="context">The parsing context.</param>
        public void Expand(IEmbeddedExpressionParser embeddedExpressionParser, ParsingContext context)
        {
            this.Name = embeddedExpressionParser.Expand(this.Name, context);
            this.SpanId = embeddedExpressionParser.Expand(this.SpanId, context);
            this.Attributes = embeddedExpressionParser.Expand(this.Attributes, context);
            this.Timestamp = embeddedExpressionParser.Expand(this.Timestamp ?? string.Empty, context);
        }
    }
}
