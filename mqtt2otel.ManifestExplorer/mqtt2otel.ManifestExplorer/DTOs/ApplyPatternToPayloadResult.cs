using mqtt2otel.Helper;
using mqtt2otel.Metadata;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using System.ComponentModel;
using System.Text;

namespace mqtt2otel.ManifestExplorer.DTOs
{
    /// <summary>
    /// Represents the result returned by the ApplyPatternToPayload service.
    /// </summary>
    public class ApplyPatternToPayloadResult
    {
        /// <summary>
        /// Gets or sets the created metrics.
        /// </summary>
        public List<MetricTestData> Metrics { get; set; } = new();

        /// <summary>
        /// Gets or sets the created logs.
        /// </summary>
        public List<LogTestData> Logs { get; set; } = new();

        /// <summary>
        /// Gets or sets the created errors.
        /// </summary>
        public List<ErrorTestData> Errors { get; set; } = new();

        /// <summary>
        /// Gets  a value indicating whether the result contains metrics.
        /// </summary>
        public bool HasMetrics { get => this.Metrics.Count > 0; }

        /// <summary>
        /// Gets a value indicating whether the reusult contains logs.
        /// </summary>
        public bool HasLogs { get=> this.Logs.Count > 0; }

        /// <summary>
        /// Gets a value indicating whether the result contains errors.
        /// </summary>
        public bool HasErrors { get => this.Errors.Count > 0; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApplyPatternToPayloadResult"/> class.
        /// 
        /// This constructor is for serialization only and should not be used directly.
        /// </summary>
        public ApplyPatternToPayloadResult()
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApplyPatternToPayloadResult"/> class.        
        /// </summary>
        /// <param name="metrics">The metrics result.</param>
        /// <param name="logs">The logs result.</param>
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

        /// <summary>
        /// Initializes a new instance of the <see cref="ApplyPatternToPayloadResult"/> class.        
        /// </summary>
        /// <param name="error">The error result.</param>
        public ApplyPatternToPayloadResult(ErrorTestData error)
        {
            this.Errors.Add(error);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApplyPatternToPayloadResult"/> class.        
        /// </summary>
        /// <param name="errors">The error result.</param>
        public ApplyPatternToPayloadResult(List<ErrorTestData> errors)
        {
            foreach (var error in errors)
            {
                this.Errors.Add(error);
            }
        }

        /// <summary>
        /// Gets the metrics as a string representation.
        /// </summary>
        /// <returns>A string representing the metrics.</returns>
        public string GetMetricsAsString()
        {
            var sb = new StringBuilder();

            foreach (var metric in this.Metrics)
            {
                sb.Append(metric.ToString());
            }
            return sb.ToString();
        }

        /// <summary>
        /// Gets the log entries as a string representation.
        /// </summary>
        /// <returns>A string representing the logs.</returns>
        public string GetLogsAsString()
        {
            var sb = new StringBuilder();

            foreach (var log in this.Logs)
            {
                sb.Append(log.ToString());
            }
            return sb.ToString();
        }

        /// <summary>
        /// Gets the error entries as a string representation.
        /// </summary>
        /// <returns>A string representing the errors.</returns>
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
