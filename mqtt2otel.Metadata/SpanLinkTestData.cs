using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace mqtt2otel.Metadata
{
    public class SpanLinkTestData
    {
        /// <summary>
        /// Gets the span id of the link target.
        /// </summary>
        public string SpanId { get; set; } = string.Empty;

        /// <summary>
        /// Gets the trace id of the link target.
        /// </summary>
        public string TraceId { get; set; } = string.Empty;

        /// <summary>
        /// Gets the tags associated with the link.
        /// </summary>
        public IEnumerable<KeyValuePair<string, object?>>? Tags { get; set; } = new List<KeyValuePair<string, object?>>();

        /// <summary>
        /// Initializes a new instance of the <see cref="SpanLinkTestData"/> class.
        /// 
        /// This constructor is for serialization only and should not be called directly.
        /// </summary>
        public SpanLinkTestData() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="SpanLinkTestData"/> class.
        /// </summary>
        public SpanLinkTestData(ActivityLink link)
        {
            this.SpanId = link.Context.SpanId.ToString();
            this.TraceId = link.Context.TraceId.ToString();
            this.Tags = link.Tags;
        }
    }
}
