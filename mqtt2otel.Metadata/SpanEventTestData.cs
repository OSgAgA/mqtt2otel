using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace mqtt2otel.Metadata
{
    public class SpanEventTestData
    {
        /// <summary>
        /// Gets the name of the event.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets the tags associated to this event.
        /// </summary>
        public IEnumerable<KeyValuePair<string, object?>> Tags { get; set; } = new List<KeyValuePair<string, object?>>();

        /// <summary>
        /// Gets the timestamp of the event.
        /// </summary>
        public DateTimeOffset Timestamp { get; set; } = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="SpanEventTestData"/> class.
        /// 
        /// This constructor is for serialization only and should not be called directly.
        /// </summary>
        public SpanEventTestData() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="SpanEventTestData"/> class.
        /// </summary>
        public SpanEventTestData(ActivityEvent evt)
        {
            this.Name = evt.Name;
            this.Tags = evt.Tags;
            this.Timestamp = evt.Timestamp;
        }
    }
}
