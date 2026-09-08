using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using mqtt2otel.Helper;
using mqtt2otel.Manifest;
using mqtt2otel.Parser;
using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
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
        /// Gets or sets the output that should be created. Set to null to not prduce any output.
        /// </summary>
        public OutputData? Output { get; set; } = null;

        /// <summary>
        /// Gets or sets a value indicating whether the open telemetry attributes should be cleared. This is executed before any new attribute
        /// is added via <see cref="AddAttributes"/>.
        /// </summary>
        public bool ClearAttributes { get; set; } = false;

        /// <summary>
        /// Gets or sets a list of open telemetry attributes that should be added to the signal. Attributes are added
        /// after <see cref="RemoveAttributes"/> have been removed. The attributes will be provided verbatim and will not be parsed.
        /// </summary>
        public List<OtelAttribute> AddAttributes { get; set; } = new();

        /// <summary>
        /// Gets or sets a list of attribute keys, that should be removed from the original message. If the key is not found
        /// on the original message, the key is ignored. Key matching is case sensitive.
        /// </summary>
        public List<string> RemoveAttributes { get; set; } = new();

        /// <summary>
        /// Applies the action on a given rule.
        /// </summary>
        /// <param name="rule">The rule to which the action should be applied.</param>
        /// <param name="name">The name of the signal</param>
        /// <param name="signalType">The type of the signal</param>
        /// <param name="expandedAttributes">The allready expanded attributes.</param>
        /// <returns>The new name, the data type, the ignored flag, the expandedAttributes and the newly created rule.</returns>
        public Tuple<string, SignalDataType, bool, IEnumerable<OtelAttribute>, OtelMetricRule> Apply(OtelMetricRule rule, string name, SignalDataType signalType, IEnumerable<OtelAttribute> expandedAttributes, IEmbeddedExpressionParser parser, ParsingContext context)
        {
            var result = rule.Clone();

            result.Unit = this.Unit == null ? rule.Unit : parser.Expand(this.Unit, context);
            result.NameFormatter = this.NameFormatter ?? rule.NameFormatter;
            result.ValueConverter = this.ValueConverter ?? rule.ValueConverter;
            result.Description = this.Description == null ? rule.Description : parser.Expand(this.Description, context);
            result.Instrument = this.Instrument ?? rule.Instrument;
            var attributes = new List<OtelAttribute>();

            if (!this.ClearAttributes)
            {
                foreach (var attribute in expandedAttributes)
                {
                    if (!this.RemoveAttributes.Contains(attribute.Key))
                    {
                        attributes.Add(attribute);
                    }
                }
            }

            foreach (var attribute in this.AddAttributes)
            {
                OtelAttribute expandedAttribute;

                if (attribute.Value != null)
                {
                    if (attribute.Value is string stringValue)
                    {
                        expandedAttribute = new OtelAttribute(attribute.Key, parser.Expand(stringValue, context));
                    }
                    else
                    {
                        expandedAttribute = new OtelAttribute(attribute.Key, attribute.Value);
                    }

                    attributes.Add(expandedAttribute);
                }
            }

            return new Tuple<string, SignalDataType, bool, IEnumerable<OtelAttribute>, OtelMetricRule>(
                this.Name == null ? name : parser.Expand(this.Name, context), 
                this.SignalDataType ?? signalType, 
                this.Ignore, 
                attributes, 
                result);
        }
    }
}
