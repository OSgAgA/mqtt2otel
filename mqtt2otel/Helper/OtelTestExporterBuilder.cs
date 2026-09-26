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
        private Dictionary<OtelServerConnection, ObservableCollection<Metric>> Metrics { get; set; } = new();

        /// <summary>
        /// Gets or sets the log entries that are collected via this exporter. You can subscribe to the observable
        /// collection to get informed about new log entries.
        /// </summary>
        private Dictionary<OtelServerConnection, ObservableCollection<LogRecord>> Logs { get; set; } = new();

        /// <summary>
        /// Gets the current scope used by the logger. Will be set via the <see cref="GetScope(LogRecordScope, object?)"/> callback.
        /// </summary>
        private Dictionary<string, object> scope = new();

        /// <inheritdoc/>
        public void AddToLoggerOptions(OpenTelemetryLoggerOptions options, OtelServerConnection connection)
        {
            if (!this.Logs.ContainsKey(connection))
            {
                this.Logs[connection] = new();
            }

            options.AddInMemoryExporter(this.Logs[connection]);
        }

        /// <inheritdoc/>
        public void AddToMeterProviderBuilder(MeterProviderBuilder builder, OtelServerConnection connection)
        {
            if (!this.Metrics.ContainsKey(connection))
            {
                this.Metrics[connection] = new();
            }

            builder.AddInMemoryExporter(this.Metrics[connection]);
        }

        /// <summary>
        /// Gets all metrics as key value pairs consisting of the connection name as key and the metric as the value.
        /// </summary>
        /// <returns>The key value pairs.</returns>
        public IEnumerable<ConnectionValue<Metric>> GetAllMetrics()
        {
            Task.Delay(10);  // Ensure, that all metrics have been processed.

            foreach (var keyValue in this.Metrics)
            {
                var connection = keyValue.Key;

                foreach (var metric in keyValue.Value)
                {
                    yield return new ConnectionValue<Metric>(connection, metric);
                }
            }
        }

        /// <summary>
        /// Gets all logs as key value pairs consisting of the connection name as key and the log entry as the value.
        /// </summary>
        /// <returns>The key value pairs.</returns>
        public IEnumerable<ConnectionValue<LogRecord>> GetAllLogs()
        {
            foreach (var keyValue in this.Logs)
            {
                var connection = keyValue.Key;

                foreach (var logEntry in keyValue.Value)
                {
                    yield return new ConnectionValue<LogRecord>(connection, logEntry);
                }
            }
        }
    }
}