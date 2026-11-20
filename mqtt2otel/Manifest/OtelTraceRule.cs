using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Text;

namespace mqtt2otel.Manifest
{
    /// <summary>
    /// Represents a rule for creating traces and spans.
    /// </summary>
    public class OtelTraceRule : Rule
    {
        /// <summary>
        /// Gets or sets information used for creating and starting new spans.
        /// </summary>
        public ImportEnabledList<OtelSpan> CreateSpans { get; set; } = new();

        /// <summary>
        /// Gets or sets the information used for ending existing spans.
        /// </summary>
        public ImportEnabledList<OtelSpan> EndSpans { get; set; } = new();

        /// <summary>
        /// Gets or sets the events that should be added to a span.
        /// </summary>
        public ImportEnabledList<OtelSpanEvent> CreateEvents { get; set; } = new();

        /// <summary>
        /// Gets or sets a list of rules that should be applied to the data after the payload parser is run.
        /// </summary>
        public ImportEnabledList<ConditionalAction<TraceThenAction>> Actions { get; set; } = new();

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
        /// Gets or sets a <see cref="TopicAttributeParser"/> pattern for parsing a topic into attributes.
        /// 
        /// Set to null if no topic parsing should be applied.
        /// </summary>
        [InheritedProperty]
        public string? TopicAttributes { get; set; } = null;

        /// <summary>
        /// Clones the rule.
        /// </summary>
        /// <returns>The cloned rule.</returns>
        public OtelTraceRule Clone()
        {
            var clone = new OtelTraceRule();

            clone.Name = this.Name;
            clone.Description = this.Description;
            clone.AllowRuleDeactivation = this.AllowRuleDeactivation;
            clone.AllowRuleDeactivationByItem = this.AllowRuleDeactivationByItem;
            clone.AllowRuleDeactivationByTopic = this.AllowRuleDeactivationByTopic;
            clone.BlockedItems = this.BlockedItems;
            clone.BlockedItemTopics = this.BlockedItemTopics;
            clone.BlockedTopicLimit = this.BlockedTopicLimit;
            clone.InternalId = this.InternalId;
            clone.IsActive = this.IsActive;
            clone.LogMessageDetails = this.LogMessageDetails;
            clone.OtelConnection = this.OtelConnection;
            clone.OtelScope = this.OtelScope;
            if (this.RetentionExceededEvent != null) clone.RetentionExceededEvent = this.RetentionExceededEvent.Clone();
            clone.RetentionInSeconds = this.RetentionInSeconds;
            clone.RetentionStrategy = this.RetentionStrategy;
            clone.WaitForLateSpanEventsInS = this.WaitForLateSpanEventsInS;
            clone.Actions = this.Actions;

            clone.CreateSpans = new();
            this.CreateSpans.ForEach(span => clone.CreateSpans.Add(span.Clone()));

            clone.EndSpans = new();
            this.EndSpans.ForEach(span => clone.EndSpans.Add(span.Clone()));

            clone.CreateEvents = new();
            this.CreateEvents.ForEach(evt => clone.CreateEvents.Add(evt.Clone()));

            return clone;
        }
    }
}
