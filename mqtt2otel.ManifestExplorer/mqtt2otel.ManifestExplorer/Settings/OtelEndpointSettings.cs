namespace mqtt2otel.ManifestExplorer.Settings
{
    /// <summary>
    /// Represents the connection to an open telemetry endpoint.
    /// </summary>
    public class OtelEndpointSettings
    {
        /// <summary>
        /// Gets or sets the uri of the endpoint.
        /// </summary>
        public string Uri { get; set; } = string.Empty;
    }
}
