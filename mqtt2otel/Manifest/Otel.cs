using System;
using System.Collections.Generic;
using System.Text;

namespace mqtt2otel.Manifest
{
    /// <summary>
    /// Provides the parent settings for open telemetry rules.
    /// </summary>
    public class Otel : NamedIdObject
    {
        /// <summary>
        /// Gets or sets the attributes that will be added to all rules.
        /// </summary>
        public List<OtelAttribute> Attributes { get; set; } = new();

        /// <summary>
        /// Gets or sets a <see cref="TopicAttributeParser"/> pattern for parsing a topic into attributes.
        /// 
        /// Set to null if no topic parsing should be applied.
        /// </summary>
        public string? TopicAttributes { get; set; } = null;

        /// <summary>
        /// Gets or sets a value indicating, whether attributes should be created from mqtt user properties (true), or not (false), or
        /// if the default setting should be used (null).
        /// </summary>
        [InheritedProperty]
        public bool? CreateAttributesFromUserProperties { get; set; } = null;

        /// <summary>
        /// Gets or sets the rules for creating open telemetry metrics.
        /// </summary>
        public ImportEnabledList<OtelMetricRule> Metrics { get; set; } = new();

        /// <summary>
        /// Gets or sets the rules for creating open telemetry metrics.
        /// </summary>
        public ImportEnabledList<OtelLoggingRule> Logs { get; set; } = new();

        /// <summary>
        /// Gets or sets the name of the open telemetriy server connection to be used for this rule. 
        /// Set to null for using the default server connection.
        /// </summary>
        [InheritedProperty]
        public string? OtelConnection { get; set; } = null;

        /// <summary>
        /// Validates the object.
        /// </summary>
        /// <param name="context">The currently active context. This will be provided as a hint to the user, where a problem occured.</param>
        /// <param name="result">The validation result.</param>
        public void Validate(string context, ValidationResult result)
        {
            this.Attributes.ForEach(attribute => attribute.Validate(context + "/Attributes", result));
            this.Metrics.ForEach( rule => rule.Validate( context + "/Metrics", result));
            this.Logs.ForEach( rule => rule.Validate( context + "/Logs", result));
        }
    }
}
