using System;
using System.Collections.Generic;
using System.Text;

namespace mqtt2otel.Shated
{
    /// <summary>
    /// Represents an otel server connection, containing the most important data for testing.
    /// </summary>
    public class OtelServerConnectionInfo
    {

        /// <summary>
        /// Gets or sets the name of the connection.
        /// </summary>
        public string Name { get; set; } = "No name";

        /// <summary>
        /// Gets or sets the service name that will be used when connecting to the server.
        /// </summary>
        public string ServiceName { get; set; } = "mqtt2otel";

        /// <summary>
        /// Gets or sets the service version that will be used when connecting to the server.
        /// </summary>
        public string ServiceVersion { get; set; } = "1.0.0";

        /// <summary>
        /// Gets or sets the service namespace.
        /// </summary>
        public string? ServiceNamespace { get; set; } = null;

        /// <summary>
        /// Gets or sets the minimum log level that will be reported to the otel endpoint.
        /// </summary>
        public string MinimumLogLevel { get; set; } = "Information";

        /// <summary>
        /// Gets or sets the endpoint address.
        /// </summary>
        public string EndpointAddress { get; set; } = "";
    }
}
