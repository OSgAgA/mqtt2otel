using mqtt2otel.Helper;
using mqtt2otel.Interfaces;
using mqtt2otel.Parser;
using mqtt2otel.Stores;
using OpenTelemetry.Context.Propagation;
using OpenTelemetry.Trace;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace mqtt2otel.Manifest
{
    /// <summary>
    /// Represents an open telemetry span.
    /// </summary>
    public class OtelSpan : NamedIdObject
    {
        /// <summary>
        /// Gets the date and time, when the object was created.
        /// </summary>
        public DateTime Created { get; private set; } = DateTime.Now;

        /// <summary>
        /// Gets the activity context of the span.
        /// </summary>
        public OtelSpanContext? Context { get; internal set; } = null;

        /// <summary>
        /// Gets the activity context of the parent if existing.
        /// </summary>
        public ActivityContext? ParentContext { get; internal set; } = null;

        /// <summary>
        /// Gets or sets the attributes associated with this span.
        /// </summary>
        public List<OtelAttribute> Attributes { get; set; } = new List<OtelAttribute>();

        /// <summary>
        /// Gets or sets the id of the span.
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the parent span. Set to null for the root span.
        /// </summary>
        public string? Parent { get; set; } = null;

        /// <summary>
        /// Backing field for <see cref="Started"/> property.
        /// </summary>
        private string? started = null;

        /// <summary>
        /// Gets or sets the date and time when the span started.
        /// </summary>
        public string? Started
        {
            get => started;
            set
            {
                started = value;
                if (value == null) this.StartedAsDateTime = null;

                if (DateTime.TryParse(value, out var result))
                {
                    this.StartedAsDateTime = result;
                }
                else
                {
                    this.StartedAsDateTime = null;
                }
            }
        }

        /// <summary>
        /// Backing field for <see cref="Completed"/> property.
        /// </summary>
        private string? completed = null;

        /// <summary>
        /// Gets or sets the date and time when the span sompleted.
        /// </summary>
        public string? Completed
        {
            get => completed;
            set
            {
                completed = value;

                if (value == null) this.CompletedAsDateTime = null;

                if (DateTime.TryParse(value, out var result))
                {
                    this.CompletedAsDateTime = result;
                }
                else
                {
                    this.CompletedAsDateTime = null;
                }
            }
        }

        /// <summary>
        /// Gets the date and time when the span started as a parsed string. If parsing is not 
        /// successful null is returned.
        /// </summary>
        public DateTime? StartedAsDateTime { get; private set; } = null;

        /// <summary>
        /// Gets the date and time when the span sompleted as a parsed string. If parsing is not 
        /// successful null is returned.
        /// </summary>
        public DateTime? CompletedAsDateTime { get; private set; } = null;

        /// <summary>
        /// Gets or sets a list of linked spans.
        /// </summary>
        public List<OtelSpanLink> LinkTo { get; set; } = new List<OtelSpanLink>();

        /// <summary>
        /// Gets or sets all span events associated with this span.
        /// </summary>
        public List<OtelSpanEvent> Events { get; set; } = new();

        /// <summary>
        /// Gets or sets the span kind.
        /// </summary>
        public ActivityKind? Kind { get; set; } = null;

        /// <summary>
        /// Gets or sets the span status.
        /// </summary>
        public ActivityStatusCode? Status { get; set; } = null;

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

        /// <summary>
        /// Gets or sets the name of the open telemetriy server connection to be used for this rule. 
        /// Set to null for using the default server connection.
        /// </summary>
        [InheritedProperty]
        public string? OtelConnection { get; set; } = null;

        /// <summary>
        /// Gets or sets the open telemetry scope that should be used for the metric.
        /// Set to null to use the default scope of the <see cref="OtelConnection"/>.
        /// </summary>
        [InheritedProperty]
        public string? OtelScope { get; set; } = null;

        /// <summary>
        /// Gets or sets a value indicating whether the mqtt message details should be logged in case of an error. Set to null to use the default value.
        /// </summary>
        [InheritedProperty]
        public bool? LogMessageDetails { get; set; } = null;

        /// <summary>
        /// Gets or sets a value indicating whether the rule can be deactivated, when the error rate exceeds <see cref="MaxErrorRateInPercentage"/>.
        /// Set to null to use default value.
        /// </summary>
        [InheritedProperty]
        public bool? AllowRuleDeactivation { get; set; } = null;

        /// <summary>
        /// Gets or sets a value indicating whether the rule can deactivate topics from executing this rule, when the error rate for the topic is to high.
        /// Set to null to use default value.
        /// </summary>
        [InheritedProperty]
        public bool? AllowRuleDeactivationByTopic { get; set; } = null;

        /// <summary>
        /// Gets or sets a value indicating whether the rule can deactivate topics from executing this rule on a scpecific item (e.g. a property of a json message), when the error rate for the topic is to high.
        /// Set to null to use default value.
        /// </summary>
        [InheritedProperty]
        public bool? AllowRuleDeactivationByItem { get; set; } = null;

        /// <summary>
        /// Gets or sets the maximum number of topics that are blocked for an item / rule. If this threshold is exceeded the whole item or rule is blocked.
        /// </summary>
        [InheritedProperty]
        public int? BlockedTopicLimit { get; set; } = null;

        /// <summary>
        /// Gets a value indicating whether the span has fully completed and can be created.
        /// </summary>
        public bool IsCompleted
        {
            get
            {
                if (this.CompletedAsDateTime == null) return false;
                if (this.CompletedAsDateTime.Value.AddSeconds(this.WaitForLateSpanEventsInS ?? 5) > DateTime.UtcNow) return false;

                return true;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the span has fully completed and can be created.
        /// </summary>
        public bool IsRetentionExceeded
        {
            get
            {
                var start = this.CompletedAsDateTime ?? this.Created;
                if (start.AddSeconds(this.RetentionInSeconds ?? 3600) > DateTime.UtcNow) return false;

                return true;
            }
        }

        /// <summary>
        /// Combines the span information with the information from another span object. 
        /// 
        /// If overrideValues is true, then the other span is treated as the leading object and overrides values of this. Otherwise
        /// this is the leading span.
        /// 
        /// Attributes and Links will be combined.
        /// 
        /// The other object remains unchanged.
        /// </summary>
        /// <param name="other">The other object.</param>
        /// <param name="overrideValues">A value indicating whether the existing values should be overriden if not null.</param>
        public void CombineWith(OtelSpan other, bool overrideValues = false)
        {
            this.Attributes = this.Attributes.Combine(other.Attributes);
            this.LinkTo.AddRange(other.LinkTo);
            this.Events.AddRange(other.Events);

            if (overrideValues) // override all values, if they are explicitly set.
            {
                if (other.Parent != null) this.Parent = other.Parent;
                if (!string.IsNullOrWhiteSpace(other.Name)) this.Name = other.Name;
                if (!string.IsNullOrWhiteSpace(other.Description)) this.Description = other.Description;
                if (other.Kind != null) this.Kind = other.Kind;
                if (other.Status != null) this.Status = other.Status;
                if (!string.IsNullOrWhiteSpace(other.Started)) this.Started = other.Started;
                if (!string.IsNullOrWhiteSpace(other.Completed)) this.Completed = other.Completed;
            }
            else // only override null values.
            {
                if (this.Parent == null) this.Parent = other.Parent;
                if (string.IsNullOrWhiteSpace(this.Name)) this.Name = other.Name;
                if (string.IsNullOrWhiteSpace(this.Description)) this.Description = other.Description;
                if (this.Kind == null) this.Kind = other.Kind;
                if (this.Status == null) this.Status = other.Status;
                if (string.IsNullOrWhiteSpace(this.Started)) this.Started = other.Started;
                if (string.IsNullOrWhiteSpace(this.Completed)) this.Completed = other.Completed;
            }
        }

        /// <summary>
        /// Applies default values to all values unset.
        /// </summary>
        public void ApplyDefaultValues()
        {
            if (string.IsNullOrWhiteSpace(this.Name)) this.Name = "No name provided.";
            if (this.Description == null) this.Description = string.Empty;
            if (this.Kind == null) this.Kind = ActivityKind.Internal;
            if (this.Status == null) this.Status = ActivityStatusCode.Unset;
            if (this.RetentionInSeconds == null) this.RetentionInSeconds = 3600;
            if (this.RetentionStrategy == null) this.RetentionStrategy = SpanRetentionStrategy.CompleteErrorWithEvent;
            if (this.RetentionExceededEvent == null) this.RetentionExceededEvent = new OtelSpanEvent() { SpanId = this.Id, Name = "Span retention time expired." };

            var now = DateTime.UtcNow;

            if (this.CompletedAsDateTime == null || this.CompletedAsDateTime > now)
            {
                this.CompletedAsDateTime = now;
                this.Attributes.Add(new OtelAttribute("mqtt2otel_error_prop_completed", "End timestamp was not set or in the future."));
            }

            if (this.StartedAsDateTime == null || this.StartedAsDateTime > now)
            {
                this.StartedAsDateTime = now;
                this.Attributes.Add(new OtelAttribute("mqtt2otel_error_prop_start", "Start timestamp was not set or in the future."));
            }

            DateTime minDate = this.StartedAsDateTime.Value;
            DateTime maxDate = this.CompletedAsDateTime.Value;

            if (this.CompletedAsDateTime < minDate) minDate = this.CompletedAsDateTime.Value;
            if (this.StartedAsDateTime > maxDate) maxDate = this.StartedAsDateTime.Value;

            foreach (var evt in this.Events)
            {
                if (evt.TimestampAsDateTime > now)
                {
                    evt.TimestampAsDateTime = now;
                    evt.Attributes.Add(new OtelAttribute("mqtt2otel_error_prop_timestamp", "Timestamp was in the future."));
                }

                if (evt.TimestampAsDateTime < minDate) minDate = evt.TimestampAsDateTime;
                if (evt.TimestampAsDateTime > maxDate) maxDate = evt.TimestampAsDateTime;
            }

            if (this.StartedAsDateTime > minDate)
            {
                this.StartedAsDateTime = minDate;
                this.Attributes.Add(new OtelAttribute("mqtt2otel_error_prop_started", "Start timestamp was smaller than end timestamp or a timestamp inside a span event."));
            }

            if (this.CompletedAsDateTime < maxDate)
            {
                this.CompletedAsDateTime = maxDate;
                this.Attributes.Add(new OtelAttribute("mqtt2otel_error_prop_completed", "End timestamp was smaller than a timestamp inside a span event."));
            }
        }

        /// <summary>
        /// Expands all values by applying the provided parser.
        /// </summary>
        /// <param name="embeddedExpressionParser">The embedded expressions parser.</param>
        /// <param name="context">The parsing context.</param>
        public void Expand(IEmbeddedExpressionParser embeddedExpressionParser, ParsingContext context)
        {
            this.Id = embeddedExpressionParser.Expand(this.Id, context);
            this.Name = embeddedExpressionParser.Expand(this.Name, context);
            this.Description = embeddedExpressionParser.Expand(this.Description, context);
            this.Attributes = embeddedExpressionParser.Expand(this.Attributes, context);
            this.Started = embeddedExpressionParser.Expand(this.Started ?? string.Empty, context);
            this.Completed = embeddedExpressionParser.Expand(this.Completed ?? string.Empty, context);

            foreach (var link in this.LinkTo)
            {
                link.Expand(embeddedExpressionParser, context);
            }

            foreach (var evt in this.Events)
            {
                evt.Expand(embeddedExpressionParser, context);
            }
        }

        /// <summary>
        /// Clones the object.
        /// </summary>
        public OtelSpan Clone()
        {
            OtelSpan clone = (OtelSpan)this.MemberwiseClone();

            var attributes = new List<OtelAttribute>();
            foreach (OtelAttribute attribute in this.Attributes)
            {
               attributes.Add(attribute.Clone());
            }

            clone.Attributes = attributes;

            clone.Events = new List<OtelSpanEvent>();
            foreach (var evt in this.Events)
            {
                clone.Events.Add(evt.Clone());
            }

            clone.LinkTo = new List<OtelSpanLink>();
            foreach (var link in this.LinkTo)
            {
                clone.LinkTo.Add(link.Clone());
            }

            return clone;
        }
    }
}
