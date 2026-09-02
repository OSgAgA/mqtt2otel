using System;
using System.Collections.Generic;
using System.Text;

namespace mqtt2otel.Manifest
{
    /// <summary>
    /// Represents the data that is needed to parse a text using a specific format.
    /// </summary>
    public class ParseAsData()
    {
        /// <summary>
        /// Gets or sets the format type as which the message should be interpreted.
        /// </summary>
        public ParseAsOptions Type { get; set; } = ParseAsOptions.Undefined;

        /// <summary>
        /// Gets or sets the separator for flattening key names.
        /// </summary>
        public string Separator { get; set; } = ".";
    }
}
