using OpenTelemetry;
using OpenTelemetry.Exporter;

namespace mqtt2otel.ManifestExplorer.Settings
{
    /// <summary>
    /// Represents the settings for connecting to an otel endpoint.
    /// </summary>
    public class OtelSettings
    {
        /// <summary>
        /// Gets or sets the service name.
        /// </summary>
        public string ServiceName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the service namespace.
        /// </summary>
        public string ServiceNamespace { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Endpoint settings.
        /// </summary>
        public OtelEndpointSettings Endpoint { get; set; } = new OtelEndpointSettings();

        /// <summary>
        /// Gets or sets the open telemetry export protocol.
        /// </summary>
        public OtlpExportProtocol OtlpExportProtocol { get; set; } = OtlpExportProtocol.Grpc;

        /// <summary>
        /// Gets or sets the open telemetry export processor type.
        /// </summary>
        public ExportProcessorType ExportProcessorType { get; set; } = ExportProcessorType.Batch;
    }
}
