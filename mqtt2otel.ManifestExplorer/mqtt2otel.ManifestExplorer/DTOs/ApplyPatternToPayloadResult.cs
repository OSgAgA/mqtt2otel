using mqtt2otel.Helper;
using mqtt2otel.Metadata;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using System.Collections.ObjectModel;
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
        public bool HasLogs { get => this.Logs.Count > 0; }

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
        /// <param name="exportBuilder">The export builder containing the metrics and log results.</param>
        /// <param name="error">The error result.</param>
        public ApplyPatternToPayloadResult(OtelTestExporterBuilder? exportBuilder = null, List<ErrorTestData>? errors = null)
        {
            this.Initialize(exportBuilder, errors);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApplyPatternToPayloadResult"/> class.        
        /// </summary>
        /// <param name="exportBuilder">The export builder containing the metrics and log results.</param>
        /// <param name="error">The error result.</param>
        private void Initialize(OtelTestExporterBuilder? exportBuilder, List<ErrorTestData>? errors)
        {
            if (errors != null)
            {
                foreach (var error in errors)
                {
                    this.Errors.Add(error);
                }
            }

            if (exportBuilder != null)
            {
                foreach (var connectionValue in exportBuilder.GetAllMetrics())
                {
                    this.Metrics.Add(new MetricTestData(connectionValue.Value, connectionValue.Connection));
                }

                foreach (var connectionValue in exportBuilder.GetAllLogs())
                {
                    this.Logs.Add(new LogTestData(connectionValue.Value, connectionValue.Connection));
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApplyPatternToPayloadResult"/> class.        
        /// </summary>
        /// <param name="exportBuilder">The export builder containing the metrics and log results.</param>
        /// <param name="error">The error result.</param>
        public ApplyPatternToPayloadResult(OtelTestExporterBuilder? exportBuilder = null, ErrorTestData? error = null)
        {
            if (error == null)
            {
                this.Initialize(exportBuilder, new List<ErrorTestData>());
            }
            else
            {
                this.Initialize(exportBuilder, new List<ErrorTestData> { error });
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApplyPatternToPayloadResult"/> class.        
        /// </summary>
        /// <param name="errors">The error result.</param>
        public ApplyPatternToPayloadResult(List<ErrorTestData> errors)
        {

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
                sb.AppendLine(error.ToString());
            }
            return sb.ToString();
        }

    }
}
