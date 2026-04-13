using System;
using System.Collections.Generic;
using System.Text;

namespace mqtt2otel.Configuration
{
    /// <summary>
    /// Provides the settings for a metrics rule.
    /// </summary>
    public class MetricsRuleSettings : NamedSetting
    {
        /// <summary>
        /// Gets or sets the otel settings for the rule.
        /// </summary>
        public OtelMetricSettings Otel { get; set; } = new ();

        /// <summary>
        /// Gets or sets the mqtt settings for the rule.
        /// </summary>
        public MqttSettings Mqtt { get; set; } = new ();

        /// <summary>
        /// Gets or sets the name of the open telemetriy server to be used for all rules in this section. 
        /// Set to null for using the default server.
        /// </summary>
        public string? OtelServerName { get; set; } = null;

        /// <summary>
        /// Validates all settings.
        /// </summary>
        /// <param name="result">The validation result.</param>
        public void Validate(ValidationResult result)
        {
            string context = $"Metric ({this.Name})";
            this.Otel.Validate(context, result);
            this.Mqtt.Validate(context, result);
        }
    }
}