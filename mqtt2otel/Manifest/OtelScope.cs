using System;
using System.Collections.Generic;
using System.Text;

namespace mqtt2otel.Manifest
{
    /// <summary>
    /// Represents an open telemetry instrumentation scope.
    /// </summary>
    public class OtelScope : NamedIdObject
    {
        /// <summary>
        /// Gets or sets the name of the open telemetriy connection to be used for this rule. 
        /// Set to null for using the default connection.
        /// </summary>
        [InheritedProperty]
        public string? OtelConnection { get; set; } = null;

        /// <summary>
        /// Gets or sets the meter version.
        /// </summary>
        public string Version { get; set; } = "1.0.0";

        /// <summary>
        /// Gets or sets attributes for this meter.
        /// </summary>
        public List<OtelAttribute> Attributes { get; set; } = new();
    }
}
