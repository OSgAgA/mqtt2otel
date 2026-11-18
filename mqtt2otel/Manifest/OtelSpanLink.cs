using mqtt2otel.Helper;
using mqtt2otel.Parser;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace mqtt2otel.Manifest
{
    /// <summary>
    /// Represents a link between two spans.
    /// </summary>
    public class OtelSpanLink
    {
        /// <summary>
        /// Gets or sets the target of the link.
        /// </summary>
        public string Target { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the attributes associated with the link.
        /// </summary>
        public List<OtelAttribute> Attributes { get; set; } = new List<OtelAttribute>();

        /// <summary>
        /// Gets the activity context of the link.
        /// </summary>
        public ActivityContext Context { get; internal set; } = new ActivityContext();

        /// <summary>
        /// Clones the object.
        /// </summary>
        /// <returns></returns>
        public OtelSpanLink Clone()
        {
            var clone = (OtelSpanLink)this.MemberwiseClone();

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
            this.Target = embeddedExpressionParser.Expand(this.Target, context);
            this.Attributes = embeddedExpressionParser.Expand(this.Attributes, context);
        }
    }
}
