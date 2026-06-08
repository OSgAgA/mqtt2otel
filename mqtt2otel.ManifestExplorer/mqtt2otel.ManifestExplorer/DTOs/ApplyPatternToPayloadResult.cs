namespace mqtt2otel.ManifestExplorer.DTOs
{
    public class ApplyPatternToPayloadResult(string metrics, string logs)
    {
        public string Metrics { get; set; } = metrics;

        public string Logs { get; set; } = logs;
    }
}
