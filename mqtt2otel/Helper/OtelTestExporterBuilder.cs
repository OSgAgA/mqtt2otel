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
    }
}