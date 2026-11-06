using mqtt2otel.Helper;
using mqtt2otel.Manifest;
using mqtt2otel.Metadata;
using mqtt2otel.Stores;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
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
        /// Gets or sets all created spans.
        /// </summary>
        public List<SpanTestData> Spans { get; set; } = new();

        /// <summary>
        /// Gets or sets the internal state of the application.
        /// </summary>
        public InternalState InternalState { get; set; } = new InternalState();

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
        /// <param name="state">The internall application state.</param>
        public ApplyPatternToPayloadResult(OtelTestExporterBuilder? exportBuilder = null, List<ErrorTestData>? errors = null, InternalState? state = null)
        {
            this.Initialize(exportBuilder, errors, state);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApplyPatternToPayloadResult"/> class.        
        /// </summary>
        /// <param name="exportBuilder">The export builder containing the metrics and log results.</param>
        /// <param name="error">The error result.</param>
        /// <param name="state">The internall application state.</param>
        private void Initialize(OtelTestExporterBuilder? exportBuilder, List<ErrorTestData>? errors, InternalState? state)
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

                foreach (var connectionValue in exportBuilder.GetAllTraces())
                {
                    this.Spans.Add(new SpanTestData(connectionValue.Value, connectionValue.Connection));
                }
            }

            if (state != null) this.InternalState = state;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApplyPatternToPayloadResult"/> class.        
        /// </summary>
        /// <param name="exportBuilder">The export builder containing the metrics and log results.</param>
        /// <param name="error">The error result.</param>
        /// <param name="state">The internall application state.</param>
        public ApplyPatternToPayloadResult(OtelTestExporterBuilder? exportBuilder = null, ErrorTestData? error = null, InternalState? state = null)
        {
            if (error == null)
            {
                this.Initialize(exportBuilder, new List<ErrorTestData>(), state);
            }
            else
            {
                this.Initialize(exportBuilder, new List<ErrorTestData> { error }, state);
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
                sb.AppendLine(error.ToString());
            }
            return sb.ToString();
        }

    }
}
