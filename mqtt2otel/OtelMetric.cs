using System;
using System.Collections.Generic;
using System.Security.Principal;
using System.Text;

namespace mqtt2otel
{
    /// <summary>
    /// Describes an open telemetry metric that can be written to an otel instrument.
    /// </summary>
    /// <typeparam name="TPayload">The type of the payload.</typeparam>
    /// <param name="value">The payload of the metric.</param>
    /// <param name="description">A human readable description of the metric.</param>
    /// <param name="unit">The units used.</param>
    /// <param name="attributes">Attributes associated with this metric.</param>
    public class OtelMetric<TPayload>(TPayload value, string description, string unit, IEnumerable<Variable> attributes)
    {
        /// <summary>
        /// Gets or sets the metric description.
        /// </summary>
        public string Description { get; set; } = description;

        /// <summary>
        /// Gets or sets the metric unit. May be empty, if no unit is available.
        /// </summary>
        public string Unit { get; set; } = unit;

        /// <summary>
        /// Gets or sets all attributes that will be applied to the metric.
        /// </summary>
        public IEnumerable<Variable> Attributes { get; set; } = attributes;

        /// <summary>
        /// Gets or sets the metric payload value.
        /// </summary>
        public TPayload Value { get; set; } = value;
    }
}
