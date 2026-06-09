using System.ComponentModel;

namespace mqtt2otel.ManifestExplorer.DTOs
{
    public class ApplyPatternToPayloadResult(string metrics, string logs, string errors = "")
    {
        public string Metrics { get; set; } = metrics;

        public string Logs { get; set; } = logs;

        public string Errors { get; set; } = errors;
    }
}
