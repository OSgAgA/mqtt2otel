using mqtt2otel.Manifest;
using mqtt2otel.Shated;
using OpenTelemetry.Metrics;
using System;
using System.Collections.Generic;
using System.Text;

namespace mqtt2otel.Helper
{
    /// <summary>
    /// Wraps a value together with open telemetry server connection information.
    /// </summary>
    /// <typeparam name="T">The type of the value that will be wrapped.</typeparam>
    /// <param name="connection">The original server connection.</param>
    /// <param name="value">The value.</param>
    public class ConnectionValue<T>(OtelServerConnection connection, T value)
    {
        /// <summary>
        /// Gets or sets the connection.
        /// </summary>
        public OtelServerConnectionInfo Connection { get; set; } = new OtelServerConnectionInfo()
        {
            Name = connection.Name,
            MinimumLogLevel = connection.MinimumLogLevel.ToString(),
            ServiceName = connection.ServiceName,
            ServiceNamespace = connection.ServiceNamespace,
            ServiceVersion = connection.ServiceVersion,
            EndpointAddress = connection.Endpoint.FullAddress
        };

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        public T Value { get; set; } = value;
    }
}
