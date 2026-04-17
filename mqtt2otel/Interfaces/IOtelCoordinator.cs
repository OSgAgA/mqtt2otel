namespace mqtt2otel.Interfaces
{
    /// <summary>
    /// The main class for communicating with the open telemetry endpoint
    /// </summary>
    public interface IOtelCoordinator : IDisposable
    {
        /// <summary>
        /// Connects to the server as described in the manifest and prepares all metrics and loggers.
        /// </summary>
        /// <param name="manifest">The manifest, contiaining the connection information.</param>
        void Connect(Manifest.Manifest manifest);
    }
}