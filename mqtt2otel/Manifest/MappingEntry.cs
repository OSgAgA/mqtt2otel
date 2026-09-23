using System;
using System.Collections.Generic;
using System.Text;

namespace mqtt2otel.Manifest
{
    /// <summary>
    /// Represents a mapping entry inside a <see cref="Mapping"/> class.
    /// </summary>
    public class MappingEntry
    {
        /// <summary>
        /// Gets or sets the source of the mapping.
        /// </summary>
        public object From { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the target of the mapping.
        /// </summary>
        public object To { get; set; } = string.Empty;
    }
}
