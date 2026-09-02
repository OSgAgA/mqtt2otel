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
        /// Gets or sets a value indicating, whether attributes should be created from mqtt user properties (true), or not (false), or
        /// if the default setting should be used (null).
        /// </summary>
        [InheritedPropertyAttribute]
        public bool? CreateAttributesFromUserProperties { get; set; } = null;

        /// <summary>
        /// Gets or sets the open telemetry instrument that will be used by the rule.
        /// </summary>
        public OtelMetricInstrument Instrument { get; set; } = OtelMetricInstrument.Gauge;

        /// <summary>
        /// Gets or sets the data type of the payload, that will be send to the otel endpoint.
        /// </summary>
        public SignalDataType SignalDataType { get; set; } = SignalDataType.Default;

        /// <summary>
        /// Gets or sets information about the unit of the <see cref="Value"/>.
        /// </summary>
        public string Unit { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets all attributes that will be applied to the metric.
        /// </summary>
        public List<OtelAttribute> Attributes { get; set; } = new();

        /// <summary>
        /// Gets or sets a <see cref="TopicAttributeParser"/> pattern for parsing a topic into attributes.
        /// 
        /// Set to null if no topic parsing should be applied.
        /// </summary>
        public string? TopicAttributes { get; set; } = null;

        /// <summary>
        /// Gets or sets a value identifying, whether and how a payload can be automatically parsed.
        /// </summary>
        public ParseAsData ParseAs { get; set; } = new ParseAsData();

        /// <summary>
        /// Gets or sets the formatter used for formatting a given key. Set to null to use original key.
        /// </summary>
        public string? NameFormatter { get; set; } = null;

        /// <summary>
        /// Gets or sets the converter used for converting a given value. Set to null to use original value.
        /// </summary>
        public string? ValueConverter { get; set; } = null;

        /// <summary>
        /// Gets or sets a list of rules that should be applied to the data after the payload parser is run, but before the value converter,
        /// or type formatter are applied.
        /// </summary>
        public List<ConditionalAction> Transformations { get; set; } = new();

        /// <summary>
        /// Gets or sets the value of the metric as a parse expression (<see cref="IPayloadParser"/>).
        /// </summary>
        public string Value { get; set; } = "Payload()";

        /// <summary>
        /// Gets or sets the name of the open telemetriy connection to be used for this rule. 
        /// Set to null for using the default connection.
        /// </summary>
        [InheritedProperty]
        public string? OtelConnection { get; set; } = null;

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

            var expression = new NCalc.Expression(this.Value);
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

        /// <summary>
        /// Provides a shallow clonw of the rule.
        /// </summary>
        /// <returns>The cloned object.</returns>
        public OtelMetricRule Clone()
        {
            var result = this.MemberwiseClone();

            return result as OtelMetricRule ?? new OtelMetricRule();
        }
    }
}
