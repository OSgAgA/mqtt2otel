using mqtt2otel.Manifest;
using System;
using System.Collections.Generic;
using System.Text;

namespace mqtt2otel
{
    /// <summary>
    /// Represents the actions that should be exeucted inside a <see cref="ConditionalAction"/>, when the condition is true.
    /// </summary>
    public class ThenAction
    {
        /// <summary>
        /// Gets or sets the name, that should be set to the resulting object. Set to null to keep the original value.
        /// </summary>
        public string? Name { get; set; } = null;

        /// <summary>
        /// Gets or sets the unit, that should be set to the resulting object. Set to null to keep the original value.
        /// </summary>
        public string? Unit { get; set; }

        /// <summary>
        /// Gets or sets the name formatter, that should be set to the resulting object. Set to null to keep the original value.
        /// </summary>
        public string? NameFormatter { get; set; }

        /// <summary>
        /// Gets or sets the value converter, that should be set to the resulting object. Set to null to keep the original value.
        /// </summary>
        public string? ValueConverter { get; set; }

        /// <summary>
        /// Gets or sets the signal type, that should be set to the resulting object. Set to null to keep the original value.
        /// </summary>
        public SignalDataType? SignalDataType { get; set; }

        /// <summary>
        /// Gets or sets the signal description.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether the rule should be fully ignored.
        /// </summary>
        public bool Ignore { get; set; } = false;

        /// <summary>
        /// Gets or sets the metric instrument. Set to null to keep the original value.
        /// </summary>
        public OtelMetricInstrument? Instrument { get; set; }

        /// <summary>
        /// Applies the action on a given rule.
        /// </summary>
        /// <param name="rule">The rule to which the action should be applied.</param>
        /// <param name="name">The name of the signal</param>
        /// <param name="signalType">The type of the signal</param>
        /// <returns>The new name, the data type, the ignored flag and the newly created rule.</returns>
        public Tuple<string, SignalDataType, bool, OtelMetricRule> Apply(OtelMetricRule rule, string name, SignalDataType signalType)
        {
            var result = rule.Clone();

            result.Unit = this.Unit ?? rule.Unit;
            result.NameFormatter = this.NameFormatter ?? rule.NameFormatter;
            result.ValueConverter = this.ValueConverter ?? rule.ValueConverter;
            result.Description = this.Description ?? rule.Description;
            result.Instrument = this.Instrument ?? rule.Instrument;

            return new Tuple<string, SignalDataType, bool, OtelMetricRule> (this.Name ?? name, this.SignalDataType ?? signalType, this.Ignore, result);
        }
    }
}
