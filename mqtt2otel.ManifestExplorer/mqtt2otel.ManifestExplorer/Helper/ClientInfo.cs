namespace mqtt2otel.ManifestExplorer.Helper
{
    /// <summary>
    /// Represents information about the client browsers that called the application.
    /// </summary>
    public class ClientInfo
    {
        /// <summary>
        /// Gets or sets the client´user agent.
        /// </summary>
        public string UserAgent { get; init; } = string.Empty;

        /// <summary>
        /// Gets or sets the platform identifier.
        /// </summary>
        public string Platform { get; init; } = string.Empty;

        /// <summary>
        /// Gets or sets the application name.
        /// </summary>
        public string AppName { get; init; } = string.Empty;

        /// <summary>
        /// Gets or sets the application version.
        /// </summary>
        public string AppVersion { get; init; } = string.Empty;

        /// <summary>
        /// Gets or sets the current browser language.
        /// </summary>
        public string Language { get; init; } = string.Empty;

        /// <summary>
        /// Gets or sets the initial width of the client window.
        /// </summary>
        public int WindowWidth { get; init; } = 0;

        /// <summary>
        /// Gets or sets the initial height of the client window.
        /// </summary>
        public int WindowHeight { get; init; } = 0;
    }
}
