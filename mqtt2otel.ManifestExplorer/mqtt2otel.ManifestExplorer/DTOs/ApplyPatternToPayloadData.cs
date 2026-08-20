using System.Numerics;

namespace mqtt2otel.ManifestExplorer.DTOs
{
    public class ApplyPatternToPayloadData(string topic, string payload, string pattern, List<UserProperty> userProperties)
    {
        public string Topic { get; set; } = topic;

        public string Payload { get; set; } = payload;

        public string Pattern { get; set; } = pattern;

        public List<UserProperty> UserProperties { get; set; } = userProperties;
    }
}
