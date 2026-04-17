using OpenTelemetry;
using OpenTelemetry.Exporter;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using NCalc;

namespace mqtt2otel.Manifest
{
    /// <summary>
    /// Provides open telemetry metric rules.
    /// </summary>
    public class OtelMetricRule : NamedIdObject
    {
        /// <summary>
        /// Gets or sets the open telemetry instrument that will be used by the rule.
        /// </summary>
        public OtelMetricInstrument Instrument { get; set; } = OtelMetricInstrument.Gauge;

        /// <summary>
        /// Gets or sets the data type of the payload, that will be send to the otel endpoint.
        /// </summary>
        public SignalDataType SignalDataType { get; set; } = SignalDataType.Float;

        /// <summary>
        /// Gets or sets information about the unit of the <see cref="Value"/>.
        /// </summary>
        public string Unit { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets all attributes that will be applied to the metric.
        /// </summary>
        public List<Variable> Attributes { get; set; } = new();

        /// <summary>
        /// Gets or sets the value of the metric as a parse expression (<see cref="IPayloadParser"/>).
        /// </summary>
        public string Value { get; set; } = "TEXT()";

        /// <summary>
        /// Gets or sets the name of the open telemetriy server to be used for this rule. 
        /// Set to null for using the default server.
        /// </summary>
        public string? OtelServerName { get; set; } = null;

        /// <summary>
        /// Gets or sets a list of bucket boundaries used in a histogram instrument. If no histogram instrument is used, this
        /// property will be ignored.
        /// </summary>
        public List<string> HistogramBucketBoundaries { get; set; } = new();

        /// <summary>
        /// Validates all objects.
        /// </summary>
        /// <param name="context">The currently active context. This will be provided as a hint to the user, where a problem occured.</param>
        /// <param name="result">The validation result.</param>
        public void Validate(string context, ValidationResult result)
        {
            this.Attributes.ForEach(attribute => attribute.Validate(context + "/Attributes", result));
            if (string.IsNullOrWhiteSpace(this.Value)) result.AddError($"{context}/({this.Name}): Value not set. Please set Value property to a non empty value.");

            var expression = new AsyncExpression(this.Value);
            if (expression.HasErrors())
            {
                if (expression.Error == null) return;

                if (expression.Error.InnerException != null)
                {
                    result.AddError($"{context}/({this.Name})/{nameof(Value)}: Expression is \"{this.Value}\". {expression.Error.InnerException.Message}");
                }
                else
                {
                    result.AddError($"{context}/({this.Name})/{nameof(Value)}: Expression is \"{this.Value}\". {expression.Error}");
                }
            }
        }
    }
}
