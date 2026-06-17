using mqtt2otel.Helper;
using mqtt2otel.Metadata;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using System.ComponentModel;
using System.Text;

namespace mqtt2otel.ManifestExplorer.DTOs
{
    public class ApplyPatternToPayloadResult
    {
        public List<MetricTestData> Metrics { get; set; } = new();

        public List<LogTestData> Logs { get; set; } = new();

        public List<ErrorTestData> Errors { get; set; } = new();

        public bool HasMetrics { get => this.Metrics.Count > 0; }

        public bool HasLogs { get=> this.Logs.Count > 0; }

        public bool HasErrors { get => this.Errors.Count > 0; }

        public ApplyPatternToPayloadResult()
        {

        }

        public ApplyPatternToPayloadResult(List<Metric> metrics, List<LogRecord> logs)
        {
            foreach (var metric in metrics)
            {
                this.Metrics.Add(new MetricTestData(metric));
            }

            foreach (var log in logs)
            {
                this.Logs.Add(new LogTestData(log));
            }
        }

        public ApplyPatternToPayloadResult(ErrorTestData error)
        {
            this.Errors.Add(error);
        }

        public ApplyPatternToPayloadResult(List<ErrorTestData> errors)
        {
            foreach (var error in errors)
            {
                this.Errors.Add(error);
            }
        }

        public string GetMetricsAsString()
        {
            var sb = new StringBuilder();

            foreach (var metric in this.Metrics)
            {
                sb.Append(metric.ToString());
            }
            return sb.ToString();
        }

        public string GetLogsAsString()
        {
            var sb = new StringBuilder();

            foreach (var log in this.Logs)
            {
                sb.Append(log.ToString());
            }
            return sb.ToString();
        }

        public string GetErrorsAsString()
        {
            var sb = new StringBuilder();

            foreach (var error in this.Errors)
            {
                sb.Append(error.ToString());
            }
            return sb.ToString();
        }

    }
}
