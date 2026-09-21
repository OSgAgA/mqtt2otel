using mqtt2otel.Metadata;
using System.Numerics;

namespace mqtt2otel.ManifestExplorer.DTOs
{
    public class ApplyPatternToPayloadData(List<MqttSetupData> mqttData, string manifest)
    {
        public List<MqttSetupData> MqttData { get; set; } = mqttData;

        public string Manifest { get; set; } = manifest;
    }
}
