using System;
using System.Collections.Generic;
using System.Text;

namespace mqtt2otel.Manifest
{
    /// <summary>
    /// Represents a mapping function, that maps a given key to a defined value.
    /// </summary>
    public class Mapping : NamedIdObject
    {
        /// <summary>
        /// Gets or sets the entries that are available for this mapping.
        /// </summary>
        public IEnumerable<MappingEntry> Entries { get; set; } = new List<MappingEntry>();
    }
}
