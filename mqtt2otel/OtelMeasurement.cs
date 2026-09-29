using mqtt2otel.Helper;
using mqtt2otel.Interfaces;
using mqtt2otel.Manifest;
using mqtt2otel.Parser;
using System;
using System.Collections.Generic;
using System.Text;

namespace mqtt2otel
{
    /// <summary>
    /// Represents a single measurement for a metric.
    /// </summary>
    public class OtelMeasurement
    {
        /// <summary>
        /// Creates a measurement from a rule. All values, that support parsers are parsed using the provided parsers.
        /// 
        /// Attention: The attributes are not set and must be set separatly.
        /// </summary>
        /// <param name="rule">The metrics rule.</param>
        /// <param name="parser">The payload parser.</param>
        /// <param name="embeddedExpressionParser">The embedded expression parser.</param>
        /// <param name="context">The parsing context.</param>
        /// <returns>The created measurement.</returns>
        public static OtelMeasurement FromRule(OtelMetricRule rule, IPayloadParser parser, IEmbeddedExpressionParser embeddedExpressionParser, ParsingContext context)
        {
            return new OtelMeasurement()
            {
                Instrument = rule.Instrument, 
                SignalDataType = rule.SignalDataType,
                Unit = embeddedExpressionParser.Expand(rule.Unit, context),
                OtelConnection = rule.OtelConnection!,
                HistogramBucketBoundaries = rule.HistogramBucketBoundaries,
                Description = embeddedExpressionParser.Expand(rule.Description, context),
                SignalName = embeddedExpressionParser.Expand(rule.Name, context),
                ValueConverter = rule.ValueConverter,
                NameFormatter = rule.NameFormatter,
                OtelScope = rule.OtelScope,
            };
        }

        /// <summary>
        /// Clones the object.
        /// </summary>
        /// <returns>The created clone.</returns>
        public OtelMeasurement Clone()
        {
            return (OtelMeasurement)MemberwiseClone();
        }

        /// <summary>
        /// Gets or sets the open telemetry instrument that will be used by the rule.
        /// </summary>
        public OtelMetricInstrument Instrument { get; set; } = OtelMetricInstrument.Gauge;

        /// <summary>
        /// Gets or sets the data type of the payload, that will be send to the otel endpoint.
        /// </summary>
        public SignalDataType SignalDataType { get; set; } = SignalDataType.Default;

        /// <summary>
        /// Gets or sets the signal name.
        /// </summary>
        public string SignalName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets information about the unit of the <see cref="Value"/>.
        /// </summary>
        public string Unit { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets all attributes that will be applied to the metric.
        /// </summary>
        public IEnumerable<OtelAttribute> Attributes { get; set; } = new List<OtelAttribute>();

        /// <summary>
        /// Gets or sets the value of the metric as a parse expression (<see cref="IPayloadParser"/>).
        /// </summary>
        public object? Value { get; set; } = 0;

        /// <summary>
        /// Gets or sets the name of the open telemetriy connection to be used for this rule. 
        /// Set to null for using the default connection.
        /// </summary>
        public string OtelConnection { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a list of bucket boundaries used in a histogram instrument. If no histogram instrument is used, this
        /// property will be ignored.
        /// </summary>
        public List<string> HistogramBucketBoundaries { get; set; } = new();

        /// <summary>
        /// Gets or sets the formatter used for formatting a given key. Set to null to use original key.
        /// </summary>
        public string? NameFormatter { get; set; } = null;

        /// <summary>
        /// Gets or sets the converter used for converting a given value. Set to null to use original value.
        /// </summary>
        public string? ValueConverter { get; set; } = null;

        /// <summary>
        /// Gets or sets a value indicating whether the measurement should be fully ignored.
        /// </summary>
        public bool Ignore { get; set; } = false;

        /// <summary>
        /// Gets or sets the open telemetry scope that should be used for the metric.
        /// Set to null to use the default scope of the <see cref="OtelConnection"/>.
        /// </summary>
        public string? OtelScope { get; set; } = null;
    }
}
