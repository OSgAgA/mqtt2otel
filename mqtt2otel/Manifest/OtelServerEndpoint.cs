using OpenTelemetry;
using OpenTelemetry.Exporter;
using System;
using System.Collections.Generic;
using System.Text;

namespace mqtt2otel.Manifest
{
    /// <summary>
    /// Provides the default settings for the open telemetry server endpoint.
    /// </summary>
    public class OtelServerEndpoint : Endpoint
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OtelServerEndpoint"/> class.
        /// </summary>
        public OtelServerEndpoint() : base()
        {
            this.Port = 4317;
            this.Protocol = "http";
        }

        /// <summary>
        /// Gets or sets the headers that the exporter will send to the server.
        /// 
        /// Set to null if no explicit headers should be send.
        /// </summary>
        public string? Headers { get; set; } = null;

        /// <summary>
        /// Gets or sets the maximum waiting time for the server to process a batch. 
        /// 
        /// Set to null to use default settings.
        /// </summary>
        public int? BatchTimeoutInMs { get; set; } = null;

        /// <summary>
        /// Gets or sets a value indicating whether transport level security should be enabled for the endpoint.
        /// </summary>
        public bool EnableTls { get; set; } = false;

        /// <summary>
        /// Gets or sets the path for a provided TLS client certificate.
        /// 
        /// Ignored if <see cref="EnableTls"/> is false.
        /// </summary>
        public string ClientCertificatePath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the password for the provided client certificate. Set to null if certificate does not need
        /// a password.
        /// 
        /// Ignored if <see cref="EnableTls"/> is false.
        /// </summary>
        public string? ClientCertificatePassword { get; set; } = null;
    }
}
