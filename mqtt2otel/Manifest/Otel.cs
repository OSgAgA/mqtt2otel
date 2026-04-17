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
        public List<Variable> Attributes { get; set; } = new();

        /// <summary>
        /// Gets or sets the rules for creating open telemetry metrics.
        /// </summary>
        public List<OtelMetricRule> Metrics { get; set; } = new();

        /// <summary>
        /// Gets or sets the rules for creating open telemetry metrics.
        /// </summary>
        public List<OtelLoggingRule> Logs { get; set; } = new();

        /// <summary>
        /// Gets or sets the name of the open telemetriy server to be used for this rule. 
        /// Set to null for using the default server.
        /// </summary>
        public string? OtelServerName { get; set; } = null;

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
