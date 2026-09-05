using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace mqtt2otel.Manifest
{
    /// <summary>
    /// Represents data that can be written to the standard output of the application.
    /// </summary>
    public class OutputData
    {
        /// <summary>
        /// Gets or sets the output text.
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the log level.
        /// </summary>
        public LogLevel Level { get; set; } = LogLevel.Information;

        /// <summary>
        /// Gets or sets additional attributes that will be added to the message.
        /// </summary>
        public Dictionary<string, object> Attributes { get; set; } = new();
    }
}
