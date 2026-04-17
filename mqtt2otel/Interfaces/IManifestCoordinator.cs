namespace mqtt2otel.Interfaces
{
    /// <summary>
    /// Coordinates all efforts regarding loading, unloading and updating of manifests.
    /// </summary>
    public interface IManifestCoordinator
    {
        /// <summary>
        /// Dispose all existing connections to the mqtt brokers and otel endpoints.
        /// </summary>
        void DisposeConnections();

        /// <summary>
        /// Process the manifest file.
        /// </summary>
        /// <returns>A value indicating whether the operation has been successfull.</returns>
        Task<bool> ProcessManifest();

        /// <summary>
        /// Registers the configures autoupdate for reloading the manifest file on changes.
        /// </summary>
        void RegisterAutoUpdater();
    }
}