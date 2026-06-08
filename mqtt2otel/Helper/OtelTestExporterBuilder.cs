using mqtt2otel.Interfaces;
using mqtt2otel.Manifest;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;

namespace mqtt2otel.Helper
{
    /// <summary>
    /// An exporter builder for testing purposes only. Will not connect to a real open telemetry endpoint.
    /// </summary>
    public class OtelTestExporterBuilder : IOtelExporterBuilder
    {
        /// <summary>
        /// Gets or sets the metrics that are collected via this exporter. You can subscribe to the observable
        /// collection to get informed about new metrics.
        /// </summary>
        public ObservableCollection<Metric> Metrics { get; set; } = new();

        /// <summary>
        /// Gets or sets the log entries that are collected via this exporter. You can subscribe to the observable
        /// collection to get informed about new log entries.
        /// </summary>
        public ObservableCollection<LogRecord> Logs { get; set; } = new();

        /// <summary>
        /// Gets the current scope used by the logger. Will be set via the <see cref="GetScope(LogRecordScope, object?)"/> callback.
        /// </summary>
        private Dictionary<string, object> scope = new();

        /// <inheritdoc/>
        public void AddToLoggerOptions(OpenTelemetryLoggerOptions options, OtelServerConnection connection)
        {
            options.AddInMemoryExporter(this.Logs);
        }

        /// <inheritdoc/>
        public void AddToMeterProviderBuilder(MeterProviderBuilder builder, OtelServerConnection connection)
        {
            builder.AddInMemoryExporter(this.Metrics);
        }

        /// <summary>
        /// Gets a string reprentation of all metrics recorded.
        /// </summary>
        /// <returns>The generated string.</returns>
        public string GetStringRepresentationOfMetrics()
        {
            var sb = new StringBuilder();

            foreach (var metric in Metrics)
            {
                sb.Append("Name: ");
                sb.AppendLine(metric.Name);

                sb.Append("  Otel server: ");
                sb.AppendLine(metric.MeterName);


                sb.Append("  Version: ");
                sb.AppendLine(metric.MeterVersion);

                sb.Append("  Description: ");
                sb.AppendLine(metric.Description);

                sb.Append("  Unit: ");
                sb.AppendLine(metric.Unit);

                sb.Append("  Metric type: ");
                sb.AppendLine(metric.MetricType.ToString());

                sb.AppendLine("  Metric points: ");
                foreach (var metricPoint in metric.GetMetricPoints())
                {
                    sb.Append("    Value:");
                    switch (metric.MetricType)
                    {
                        case MetricType.LongSum:
                            sb.AppendLine(metricPoint.GetSumLong().ToString());
                            break;
                        case MetricType.DoubleSum:
                            sb.AppendLine(metricPoint.GetSumDouble().ToString());
                            break;
                        case MetricType.LongGauge:
                            sb.AppendLine(metricPoint.GetGaugeLastValueLong().ToString());
                            break;
                        case MetricType.DoubleGauge:
                            sb.AppendLine(metricPoint.GetGaugeLastValueDouble().ToString());
                            break;
                        case MetricType.Histogram:
                            sb.AppendLine(metricPoint.GetHistogramSum().ToString());
                            break;
                        case MetricType.ExponentialHistogram:
                            sb.AppendLine(metricPoint.GetExponentialHistogramData().ToString());
                            break;
                        case MetricType.LongSumNonMonotonic:
                            sb.AppendLine(metricPoint.GetSumLong().ToString());
                            break;
                        case MetricType.DoubleSumNonMonotonic:
                            sb.AppendLine(metricPoint.GetSumDouble().ToString());
                            break;
                        default:
                            sb.AppendLine($"Unknown data type: {metric.MetricType}");
                            break;
                    }

                    sb.AppendLine($"    Start time: {metricPoint.StartTime}");
                    sb.AppendLine($"    End time: {metricPoint.EndTime}");

                    if (metricPoint.Tags.Count > 0)
                    {
                        sb.AppendLine("    Attributes:");
                        foreach (var tag in metricPoint.Tags)
                        {
                            sb.AppendLine("      " + tag.Key + ": " + tag.Value);
                        }
                    }
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Gets a string reprentation of all log entries recorded.
        /// </summary>
        /// <returns>The generated string.</returns>
        public string GetStringRepresentationOfLogEntries()
        {
            var sb = new StringBuilder();

            foreach (var entry in this.Logs)
            {
                sb.Append("Timestamp: ");
                sb.AppendLine(entry.Timestamp.ToString());

                sb.Append("  Level: ");
                sb.AppendLine(entry.LogLevel.ToString());

                sb.Append("  Body: ");
                sb.AppendLine(entry.Body);

                sb.Append("  Category name: ");
                sb.AppendLine(entry.CategoryName);

                if (entry.Attributes != null)
                {
                    sb.AppendLine("  Attributes: ");
                    this.AddAttributesToString(entry, sb);
                }

                sb.Append("  Span id: ");
                sb.AppendLine(entry.SpanId.ToString());

                sb.Append("  Trace id: ");
                sb.AppendLine(entry.TraceId.ToString());

                sb.Append("  Trace state: ");
                sb.AppendLine(entry.TraceState);

                sb.Append("  Trace state: ");
                sb.AppendLine(entry.TraceFlags.ToString());
            }

            return sb.ToString();
        }

        /// <summary>
        /// Called before the log message is written. Will override the timestamp, when a supported timestamp key is found
        /// inside the logging scope. 
        /// </summary>
        /// <param name="record">The log record.</param>
        public void AddAttributesToString(LogRecord record, StringBuilder sb)
        {
            record.ForEachScope<object?>((scope, state) => this.GetScope(scope, state), null);

            foreach (var attribute in this.scope)
            {
                sb.AppendLine($"    {attribute.Key}: {attribute.Value}");
            }
        }

        /// <summary>
        /// Called from ForEachScope to gather scope inforamation for the current loger.
        /// </summary>
        /// <param name="scope">The scope that should be added.</param>
        /// <param name="state">The current state. Will be ignored.</param>
        private void GetScope(LogRecordScope scope, object? state)
        {
            if (scope.Scope is List<KeyValuePair<string, object>> typedScope && typedScope != null)
            {
                this.scope = typedScope.ToDictionary<string, object>();
            }
        }
    }
}