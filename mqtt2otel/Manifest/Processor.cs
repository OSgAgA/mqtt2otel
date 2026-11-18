using Microsoft.Extensions.Logging;
using mqtt2otel.Helper;
using mqtt2otel.Interfaces;
using mqtt2otel.InternalLogging;
using mqtt2otel.InternalMetrics;
using mqtt2otel.Parser;
using mqtt2otel.Stores;
using mqtt2otel.Transformation;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml.Linq;
using YamlDotNet.Core.Tokens;

namespace mqtt2otel.Manifest
{
    /// <summary>
    /// Represents a processor. A processor is responsible for subscribing to mqtt topics and
    /// applying otel rules to these subscriptions.
    /// </summary>
    public class Processor : NamedIdObject
    {

        /// <summary>
        /// Gets or sets a value indicating, whether attributes should be created from mqtt user properties (true), or not (false), or
        /// if the default setting should be used (null).
        /// </summary>
        [InheritedProperty]
        public bool? CreateAttributesFromUserProperties { get; set; } = null;

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
        /// Gets or sets the otel settings for the processor.
        /// </summary>
        public Otel Otel { get; set; } = new();

        /// <summary>
        /// Gets or sets the mqtt settings for the processor.
        /// </summary>
        public Mqtt Mqtt { get; set; } = new();

        /// <summary>
        /// Gets or sets the name of the open telemetriy connection to be used for all processors in this section. 
        /// Set to null for using the default connection.
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
        /// Validates the object.
        /// </summary>
        /// <param name="result">The validation result.</param>
        public void Validate(ValidationResult result)
        {
            string context = $"Processor ({this.Name})";
            this.Otel.Validate(context, result);
            this.Mqtt.Validate(context, result);
        }
    }
}