using mqtt2otel.Helper;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using System;
using System.Collections.Generic;
using System.Text;

namespace mqtt2otel.Configuration
{
    /// <summary>
    /// Provides settings for configuring the open telemetry server.
    /// </summary>
    public class OtelServerSettings : NamedSetting
    {
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
        /// Gets or sets the open telemetry server endpoint.
        /// </summary>
        public OtelServerEndpointSettings Endpoint { get; set; } = new ();

        /// <summary>
        /// Gets or sets a prefix that will be prepended to the client id used to connect to the server. This helps in tracking down issues,
        /// when mutliple connections to clients are active.
        /// </summary>
        public string ClientPrefix { get; set; } = ApplicationStringConstants.ApplicationName;


        /// <summary>
        /// Gets or sets the export protocoll that should be used when connecting to the server.
        /// </summary>
        public OtlpExportProtocol OtlpExportProtocol { get; set; } = OtlpExportProtocol.Grpc;

        /// <summary>
        /// Gets or sets the export processor type used when connecting to the server.
        /// </summary>
        public ExportProcessorType ExportProcessorType { get; set; } = ExportProcessorType.Batch;

        /// <summary>
        /// Validates all settings.
        /// </summary>
        /// <param name="result">The validation results.</param>
        public void Validate(ValidationResult result)
        {
            this.Endpoint.Validate("Otel server", result);
        }
    }
}
