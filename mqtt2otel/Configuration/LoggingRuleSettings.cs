using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;

namespace mqtt2otel.Configuration
{
    /// <summary>
    /// Provides the available settings for a logging rule.
    /// </summary>
    public class LoggingRuleSettings : NamedSetting
    {
        /// <summary>
        /// Gets or sets the open telemetry logging settings.
        /// </summary>
        public OtelLoggingSettings Otel { get; set; } = new();

        /// <summary>
        /// Gets or sets the mqtt settings.
        /// </summary>
        public MqttSettings Mqtt { get; set; } = new ();

        /// <summary>
        /// The key for structured payloads (e.g. json) to be used to identify the message body.
        /// </summary>
        public string MessageKey { get; set; } = "otel_message";

        /// <summary>
        /// The key for structured payloads (e.g. json) to be used to identify the log level.
        /// </summary>
        public string LogLevelKey { get; set; } = "otel_loglevel";

        /// <summary>
        /// Validates all settings.
        /// </summary>
        /// <param name="result">The validation results.</param>
        public void Validate(ValidationResult result)
        {
            this.Mqtt.Validate($"Log ({this.Name})", result);
        }
    }
}
