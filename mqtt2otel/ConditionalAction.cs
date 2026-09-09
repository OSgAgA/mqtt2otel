using mqtt2otel.Manifest;
using System;
using System.Collections.Generic;
using System.Text;

namespace mqtt2otel
{
    /// <summary>
    /// Represents a rule, that checks for a given condition and, if successful, applies the given action.
    /// </summary>
    public class ConditionalAction : NamedIdObject
    {
        /// <summary>
        /// Gets or sets the condition that must be matched to apply the <see cref="ThenAction"/>.
        /// </summary>
        public List<string> When { get; set; } = new();

        /// <summary>
        /// Gets or sets the action that should be applied, when the condition matches.
        /// </summary>
        public ThenAction Then { get; set; } = new();
    }
}
