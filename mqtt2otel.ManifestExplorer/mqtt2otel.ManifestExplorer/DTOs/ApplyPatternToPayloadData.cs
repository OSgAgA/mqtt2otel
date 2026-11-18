using mqtt2otel.Metadata;
using mqtt2otel.Provider;
using mqtt2otel.Stores;
using System.Numerics;

namespace mqtt2otel.ManifestExplorer.DTOs
{
    /// <summary>
    /// Represents the pattern that should be applied for the given message.
    /// </summary>
    /// <param name="mqttData">The mqtt data received from the broker.</param>
    /// <param name="manifest">The manifest to which the mqtt data should be applied.</param>
    /// <param name="state">The current state of the application.</param>
    public class ApplyPatternToPayloadData(MqttSetupData mqttData, string manifest, InternalState state)
    {
        /// <summary>
        /// Gets or sets the mqtt data received from the broker.
        /// </summary>
        public MqttSetupData MqttData { get; set; } = mqttData;

        /// <summary>
        /// Gets or sets the manifest to which the mqtt data should be applied.
        /// </summary>
        public string Manifest { get; set; } = manifest;

        /// <summary>
        /// Gets or sets the internal state of the application.
        /// </summary>
        public InternalState State { get; set; } = state;

        /// <summary>
        /// Gets or sets a value indicating whether fake data providers should be used instead of the default ones.
        /// 
        /// When set to null the value depends on whether the application is in test mode or not.
        /// </summary>
        public bool? UseFakeProviders { get; set; } = null;
    }
}
