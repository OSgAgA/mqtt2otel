using mqtt2otel.Metadata;
using mqtt2otel.Stores;
using System.Numerics;

namespace mqtt2otel.ManifestExplorer.DTOs
{
    /// <summary>
    /// Represents the pattern that should be applied for the given message.
    /// </summary>
    /// <param name="mqttData">The mqtt data received from the broker.</param>
    /// <param name="manifest">The manifest to which the mqtt data should be applied.</param>
    public class ApplyPatternToPayloadData(MqttSetupData mqttData, string manifest)
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
        public InternalState State { get; set; } = new InternalState();
    }
}
