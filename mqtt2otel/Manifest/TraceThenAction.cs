using mqtt2otel.Helper;
using mqtt2otel.Interfaces;
using mqtt2otel.Parser;
using OpenTelemetry.Logs;
using System;
using System.Collections.Generic;
using System.Text;

namespace mqtt2otel.Manifest
{
    /// <summary>
    /// Represents the actions that should be exeucted inside a <see cref="ConditionalAction"/> inside a trace rule, when the condition is true.
    /// </summary>
    public class TraceThenAction
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
        public List<OtelSpanEvent> CreateEvents { get; set; } = new();

        /// <summary>
        /// Gets or sets a value indicating whether the created span list should be cleared before adding new create spans.
        /// </summary>
        public bool ClearCreatedSpans { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether the ended span list should be cleared before adding new end spans.
        /// </summary>
        public bool ClearEndedSpans { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether the event list should be cleared before adding new events.
        /// </summary>
        public bool ClearEvents { get; set; } = false;


        /// <summary>
        /// Applies the action on a given rule.
        /// </summary>
        /// <param name="rule">The otel trace rule, to which the actions should be applied.</param>
        /// <param name="embeddedParser">The embedded expression parser.</param>   
        /// <param name="parser">The expression parser.</param>   
        /// <param name="context">The current parsing context.</param>
        public void Apply(OtelTraceRule rule, IEmbeddedExpressionParser embeddedParser, IPayloadParser parser, ParsingContext context)
        {
            if (this.ClearCreatedSpans) rule.CreateSpans.Clear();
            if (this.ClearEndedSpans) rule.EndSpans.Clear();
            if (this.ClearEvents) rule.CreateEvents.Clear();

            foreach (var span in this.CreateSpans)
            {
                var clone = span.Clone();
                if (clone.OtelConnection == null) clone.OtelConnection = rule.OtelConnection;
                if (clone.WaitForLateSpanEventsInS == null) clone.WaitForLateSpanEventsInS = rule.WaitForLateSpanEventsInS;
                if (clone.RetentionExceededEvent == null) clone.RetentionExceededEvent = rule.RetentionExceededEvent;
                if (clone.RetentionInSeconds == null) clone.RetentionInSeconds = rule.RetentionInSeconds;
                if (clone.RetentionStrategy == null) clone.RetentionStrategy = rule.RetentionStrategy;
                rule.CreateSpans.Add(clone);
            }

            foreach (var span in this.EndSpans)
            {
                var clone = span.Clone();
                if (clone.OtelConnection == null) clone.OtelConnection = rule.OtelConnection;
                if (clone.WaitForLateSpanEventsInS == null) clone.WaitForLateSpanEventsInS = rule.WaitForLateSpanEventsInS;
                if (clone.RetentionExceededEvent == null) clone.RetentionExceededEvent = rule.RetentionExceededEvent;
                if (clone.RetentionInSeconds == null) clone.RetentionInSeconds = rule.RetentionInSeconds;
                if (clone.RetentionStrategy == null) clone.RetentionStrategy = rule.RetentionStrategy;
                rule.EndSpans.Add(clone);
            }

            foreach (var evt in this.CreateEvents)
            {
                var clone = evt.Clone();
                if (clone.OtelConnection == null) clone.OtelConnection = rule.OtelConnection;
                rule.CreateEvents.Add(clone);
            }
        }
    }
}
