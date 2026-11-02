using mqtt2otel.Manifest;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace mqtt2otel.Interfaces
{
    /// <summary>
    /// Represents classes that can add open telemetry exporters to metrics and loggers.
    /// </summary>
    public interface IOtelExporterBuilder
    {
        /// <summary>
        /// Adds the exporter to the provided logger options.
        /// </summary>
        /// <param name="options">The open telemetry logger opions.</param>
        /// <param name="connection">The otel server connection data.</param>
        void AddToLoggerOptions(OpenTelemetryLoggerOptions options, OtelServerConnection connection);

        /// <summary>
        /// Adds the exporter to the provided meter builder.
        /// </summary>
        /// <param name="options">The open telemetry logger opions.</param>
        /// <param name="connection">The otel server connection data.</param>
        void AddToMeterProviderBuilder(MeterProviderBuilder builder, OtelServerConnection connection);

        /// <summary>
        /// Adds the exporter to the provided tracer builder.
        /// </summary>
        /// <param name="options">The open telemetry trace builder.</param>
        /// <param name="connection">The otel server connection data.</param>
        public void AddToTracerProviderBuilder(TracerProviderBuilder builder, OtelServerConnection connection);
    }
}