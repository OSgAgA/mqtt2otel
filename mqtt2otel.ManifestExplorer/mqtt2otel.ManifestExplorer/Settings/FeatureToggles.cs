namespace mqtt2otel.ManifestExplorer.Settings
{
    /// <summary>
    /// Contains all supported feature toggles of the manifest explorer.
    /// </summary>
    public class FeatureToggles
    {
        /// <summary>
        /// Gets or sets a value indicating whether the create json button should be shown on the home page.
        /// </summary>
        public bool ShowCreateJsonButton { get; set; } = false;
    }
}
