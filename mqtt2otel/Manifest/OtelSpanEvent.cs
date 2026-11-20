using mqtt2otel.Helper;
using mqtt2otel.Interfaces;
using mqtt2otel.Parser;
using System;
using System.Collections.Generic;
using System.Text;

namespace mqtt2otel.Manifest
{
    /// <summary>
    /// Represents an open telemetry span event.
    /// </summary>
    public class OtelSpanEvent : Rule
    {
        /// <summary>
        /// Gets or sets the span id of the event.
        /// </summary>
        public string SpanId { get; set; } = string.Empty;

        /// <summary>
        /// Backing field for <see cref="Completed"/> property.
        /// </summary>
        private string? timestamp = null;

        /// <summary>
        /// Gets or sets a <see cref="TopicAttributeParser"/> pattern for parsing a topic into attributes.
        /// 
        /// Set to null if no topic parsing should be applied.
        /// </summary>
        [InheritedProperty]
        public string? TopicAttributes { get; set; } = null;

        /// <summary>
        /// Gets or sets the timestamp, when the event happened. Defaults to DateTime.Now.
        /// </summary>
        public string? Timestamp
        {
            get => timestamp;
            set
            {
                timestamp = value;

                if (value == null)
                {
                    return;
                }
                else if (DateTimeHelper.TryParseToUtc(value, out var result))
                {
                    this.TimestampAsDateTime = result;
                }
                else
                {
                    return;
                }
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
        public void Expand(IPayloadParser parser, IEmbeddedExpressionParser embeddedExpressionParser, ParsingContext context)
        {
            this.Name = embeddedExpressionParser.Expand(this.Name, context);
            this.SpanId = embeddedExpressionParser.Expand(this.SpanId, context);
            this.Attributes = embeddedExpressionParser.Expand(this.Attributes, context);
            if (this.TopicAttributes != null)
            {
                this.Attributes = this.Attributes.Combine(TopicAttributeParser.Parse(context.Message.Topic, this.TopicAttributes));
            }

            this.Timestamp = embeddedExpressionParser.Expand(this.Timestamp ?? string.Empty, context);
        }

        /// <summary>
        /// Gets the default retention exceeded event for the span with the provided id.
        /// </summary>
        /// <param name="spanId">The id of the span, that holds the event.</param>
        /// <returns>The default retention exceeded event.</returns>
        public static OtelSpanEvent DefaultRetentionExceededEvent
        {
            get => new OtelSpanEvent() { Name = "Span retention time expired." };
        }
    }
}
