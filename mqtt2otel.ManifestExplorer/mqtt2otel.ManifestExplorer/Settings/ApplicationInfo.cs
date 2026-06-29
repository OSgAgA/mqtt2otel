namespace mqtt2otel.ManifestExplorer.Settings
{
    /// <summary>
    /// Provides general information about the application.
    /// </summary>
    public class ApplicationInfo
    {
        /// <summary>
        /// Gets the version of the application. Will be N/A if not explicitly set via the file version.txt.
        /// </summary>
        public string Version { get; private set; } = "N/A";

        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationInfo"/> class.
        /// </summary>
        /// <param name="pathToVersionFile">The path to the version file. If not found, N/A will be set as a default value. The file must contain at least
        /// one line. The first line is used as the version identifier.</param>
        public ApplicationInfo(string pathToVersionFile)
        {
            if (Path.Exists(pathToVersionFile))
            {
                var lines = File.ReadLines(pathToVersionFile);
                if (lines.Count() > 0) this.Version = lines.First();
            }
        }
    }
}
