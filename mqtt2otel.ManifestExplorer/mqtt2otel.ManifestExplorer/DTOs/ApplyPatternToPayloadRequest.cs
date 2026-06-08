using System.Numerics;

namespace mqtt2otel.ManifestExplorer.DTOs
{
    public class ApplyPatternToPayloadRequest(string topic, string payload, string pattern)
    {
        public string Topic { get; set; } = topic;

        public string Payload { get; set; } = payload;

        public string Pattern { get; set; } = pattern;
    }
}
