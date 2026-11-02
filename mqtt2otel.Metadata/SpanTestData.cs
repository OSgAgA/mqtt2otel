using mqtt2otel.Shated;
using OpenTelemetry.Metrics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Quic;
using System.Text;

namespace mqtt2otel.Metadata
{
    public class SpanTestData
    {
        /// <summary>
        /// Gets or sets the server connection.
        /// </summary>
        public OtelServerConnectionInfo Connection { get; set; } = new();

        /// <summary>
        /// Gets the start time of the span in utc.
        /// </summary>
        public DateTime StartUtc { get; set; } = DateTime.Now;

        /// <summary>
        /// Gets the tags associated with the span.
        /// </summary>
        public IEnumerable<KeyValuePair<string, string?>> Tags { get; set; } = new List<KeyValuePair<string, string?>>();

        /// <summary>
        /// Gets the span duration.
        /// </summary>
        public TimeSpan Duration { get; set; } = new();

        /// <summary>
        /// Gets the trace id of the span.
        /// </summary>
        public string TraceId { get; set; } = string.Empty;

        /// <summary>
        /// Gets the span id.
        /// </summary>
        public string SpanId { get; set; } = string.Empty;

        /// <summary>
        /// Gets the span id of the parent.
        /// </summary>
        public string? ParentSpanId { get; set; } = null;

        /// <summary>
        /// Gets the span name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets the span kind.
        /// </summary>
        public string Kind { get; set; } = string.Empty;

        /// <summary>
        /// Gets the span status.
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Gets the events associated with the span.
        /// </summary>
        public List<SpanEventTestData> Events { get; set; } = new();

        /// <summary>
        /// Gets the links associated with the span.
        /// </summary>
        public List<SpanLinkTestData> Links { get; set; } = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="SpanTestData"/> class.
        /// 
        /// This constructor is for serialization only and should not be called directly.
        /// </summary>
        public SpanTestData() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="SpanTestData"/> class.
        /// </summary>
        /// <param name="activity">The acrivity representing the expected result.</param>
        /// <param name="connection">The open telemetry connection name.</param>
        public SpanTestData(Activity activity, OtelServerConnectionInfo connection)
        {
            this.Connection = connection;
            this.StartUtc = activity.StartTimeUtc;
            this.Tags = activity.Tags;
            this.Duration = activity.Duration;
            this.TraceId = activity.TraceId.ToString();
            this.SpanId = activity.SpanId.ToString();
            this.ParentSpanId = activity.ParentSpanId.ToString();
            this.Name = activity.OperationName;
            this.Kind = activity.Kind.ToString();
            this.Status = activity.Status.ToString();
            
            foreach (var evt in activity.Events)
            {
                this.Events.Add(new SpanEventTestData(evt));
            }

            foreach (var link in activity.Links)
            {
                this.Links.Add(new SpanLinkTestData(link));
            }
        }

    }
}
