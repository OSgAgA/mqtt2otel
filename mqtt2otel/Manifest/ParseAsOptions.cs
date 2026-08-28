using System;
using System.Collections.Generic;
using System.Text;

namespace mqtt2otel.Manifest
{
    /// <summary>
    /// Represents all options for automatic parsing a payload.
    /// </summary>
    public enum ParseAsOptions
    {
        /// <summary>
        /// Payload is undefined and cannot be automatically parsed.
        /// </summary>
        Undefined,

        /// <summary>
        /// Payload is a Json.
        /// </summary>
        Json
    }
}
