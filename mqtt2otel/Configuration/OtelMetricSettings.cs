using System;
using System.Collections.Generic;
using System.Text;

namespace mqtt2otel.Configuration
{
    /// <summary>
    /// Provides the parent settings for open telemetry rules.
    /// </summary>
    public class OtelMetricSettings : NamedSetting
    {
        /// <summary>
        /// Gets or sets the attributes that will be added to all rules inside these settings.
        /// </summary>
        public List<Variable> Attributes { get; set; } = new();

        /// <summary>
        /// Gets or sets the rules for creating open telemetry metrics.
        /// </summary>
        public List<OtelMetricRuleSettings> Rules { get; set; } = new();

        /// <summary>
        /// Gets or sets the name of the open telemetriy server to be used for this rule. 
        /// Set to null for using the default server.
        /// </summary>
        public string? OtelServerName { get; set; } = null;

        /// <summary>
        /// Validates all settings.
        /// </summary>
        /// <param name="context">The currently active context. This will be provided as a hint to the user, where a problem occured.</param>
        /// <param name="result">The validation result.</param>
        public void Validate(string context, ValidationResult result)
        {
            this.Attributes.ForEach(attribute => attribute.Validate(context + " / Attributes", result));
            this.Rules.ForEach( rule => rule.Validate( context + " / Rules", result));
        }
    }
}
