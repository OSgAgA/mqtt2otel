using System;
using System.Collections.Generic;
using System.Text;

namespace mqtt2otel.Manifest
{
    /// <summary>
    /// Provides an open telemetry logging rules.
    /// </summary>
    public class OtelLoggingRule : NamedIdObject
    {
        /// <summary>
        /// Gets or sets the category name, that should be used by the logger.
        /// </summary>
        public string CategoryName { get; set; } = "mqtt2otel";

        /// <summary>
        /// Gets or sets a filter expression (<see cref="Parser.PayloadParser"/> for the processed payload.
        /// 
        /// This filter is applied to every payload received by the logger and reduces the data to the result
        /// of the expression. This will be applied after <see cref="Transform"/>.
        /// </summary>
        public string Filter { get; set; } = "TEXT:.";

        /// <summary>
        /// Gets or sets all attributes that should be added to the open telemetry log message.
        /// </summary>
        public List<Variable> Attributes { get; set; } = new();

        /// <summary>
        /// Gets or sets the data type of the payload that will be used to write to the open telemetry logs.
        /// See <see cref="OtelLoggingPayloadType"/> for further details.
        /// </summary>
        public OtelLoggingPayloadType PayloadType { get; set; } = OtelLoggingPayloadType.Text;

        /// <summary>
        /// Gets or sets a transformation expression (<see cref="Transformation.PayloadTransformation"/>) that will be applied to the payload before 
        /// the <see cref="Filter"/> expression.
        /// 
        /// Leave empty to not apply any transformation to the payload.
        /// </summary>
        public string Transform { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the name of the open telemetriy server to be used for this rule. 
        /// Set to null for using the default server.
        /// </summary>
        public string? OtelServerName { get; set; } = null;

        /// <summary>
        /// The key for structured payloads (e.g. json) to be used to identify the message body.
        /// </summary>
        public string MessageKey { get; set; } = "otel_message";

        /// <summary>
        /// The key for structured payloads (e.g. json) to be used to identify the log level.
        /// </summary>
        public string LogLevelKey { get; set; } = "otel_loglevel";

        /// <summary>
        /// Validates the object.
        /// </summary>
        /// <param name="context">The currently active context. This will be provided as a hint to the user, where a problem occured.</param>
        /// <param name="result">The validation result.</param>
        public void Validate(string context, ValidationResult result)
        {
            if (string.IsNullOrWhiteSpace(this.Filter)) result.AddError($"{this.Name}: Filter may not be empty.");
            this.Attributes.ForEach(attribute => attribute.Validate($"{this.Name} / Attributes", result));
        }
    }
}
