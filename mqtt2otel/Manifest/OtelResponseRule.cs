using System;
using System.Collections.Generic;
using System.Text;

namespace mqtt2otel.Manifest
{
    /// <summary>
    /// Represents a rule, that is triggered viy a mqtto response topic.
    /// </summary>
    public class OtelResponseRule : Otel
    {
        /// <summary>
        /// Gets or sets a list of conidtions that must be meat for applying this rule.
        /// </summary>
        public List<string> When { get; set; } = new();
    }
}
