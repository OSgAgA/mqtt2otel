using mqtt2otel.Manifest;
using OpenTelemetry.Trace;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace mqtt2otel.Stores
{
    /// <summary>
    /// Represents the open telemetry span context.
    /// </summary>
    public class OtelSpanContext
    {
        /// <summary>
        /// Gets or sets the internal id of the span.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the date and time when the span was sent to the otel backend.
        /// 
        /// Null indicates, that this span has not been sent yet.
        /// </summary>
        public DateTime? Sent { get; set; } = null;

        /// <summary>
        /// Gets or sets the trace id.
        /// </summary>
        public string TraceId { get; set; } = ActivityTraceId.CreateRandom().ToString();

        /// <summary>
        /// Gets or sets the span id.
        /// </summary>
        public string SpanId { get; set; } = ActivitySpanId.CreateRandom().ToString();

        /// <summary>
        /// Gets or sets the timespan for how long a span will be kept alive.
        /// </summary>
        public int RetentionInSeconds { get; set; } = 3600;

        /// <summary>
        /// Gets or sets the <see cref="SpanRetentionStrategy"/> used, when the retention time is exceeded.
        /// </summary>
        public SpanRetentionStrategy RetentionStrategy { get; set; } = SpanRetentionStrategy.CompleteErrorWithEvent;

        /// <summary>
        /// Gets or sets the event that will be created, when the retention time is exceeded. Only applies if a strategy is chosen, 
        /// that creates events.
        /// </summary>
        public OtelSpanEvent RetentionExceededEvent { get; set; } = new OtelSpanEvent() { Name = "Retention time for span exceeded." };

        /// <summary>
        /// Gets the date and time, when this context was created.
        /// </summary>
        public DateTime Created { get; } = DateTime.UtcNow;

        /// <summary>
        /// Gets a vaule indicating, whether the retention period is expired.
        /// </summary>
        public bool IsExpired { get { return DateTime.UtcNow > this.Created.AddSeconds(this.RetentionInSeconds); } }

        /// <summary>
        /// Converts this object to an <see cref="ActivityContext"/>.
        /// </summary>
        /// <returns></returns>
        public ActivityContext ToActivityContext()
        {
            return new ActivityContext(ActivityTraceId.CreateFromString(this.TraceId), ActivitySpanId.CreateFromString(this.SpanId), ActivityTraceFlags.Recorded);
        }
    }
}
