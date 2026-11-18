using System;
using System.Collections.Generic;
using System.Text;

namespace mqtt2otel.Manifest
{
    /// <summary>
    /// Represents the available otel trace status codes.
    /// </summary>
    public enum TraceStatusCode
    {
        /// <summary>
        /// The default value. 
        /// </summary>
        Unset,

        /// <summary>
        /// Status ok.
        /// </summary>
        Ok,

        /// <summary>
        /// Status error.
        /// </summary>
        Error
    }
}
