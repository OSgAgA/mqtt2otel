using mqtt2otel.Interfaces;
using mqtt2otel.Manifest;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace mqtt2otel
{
    /// <summary>
    /// Provides the standard open telemetry exporter.
    /// </summary>
    public class OtelExporterBuilder : IOtelExporterBuilder
    {
        /// <summary>
        /// Adds the exporter to the provided logger options.
        /// </summary>
        /// <param name="options">The open telemetry logger opions.</param>
        /// <param name="connection">The otel server connection data.</param>
        public void AddToLoggerOptions(OpenTelemetryLoggerOptions options, OtelServerConnection connection)
        {
            options.AddOtlpExporter(otlpOptions => this.InitializeExporterOptions(otlpOptions, connection));
        }

        /// <summary>
        /// Adds the exporter to the provided meter builder.
        /// </summary>
        /// <param name="options">The open telemetry logger opions.</param>
        /// <param name="connection">The otel server connection data.</param>
        public void AddToMeterProviderBuilder(MeterProviderBuilder builder, OtelServerConnection connection)
        {
            builder.AddOtlpExporter(otlpOptions => this.InitializeExporterOptions(otlpOptions, connection));
        }

        /// <summary>
        /// Initializes <see cref="OtlpExporterOptions"/> based on the provided settings.
        /// </summary>
        /// <param name="otlpOptions">The options that will be initialized.</param>
        /// <param name="connection">The settings defining the options to be applied.</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private OtlpExporterOptions InitializeExporterOptions(OtlpExporterOptions otlpOptions, OtelServerConnection connection)
        {
            if (connection.Endpoint.Address == null) throw new Exception("Address of Otel server endpoint must be set!");

            otlpOptions.Endpoint = connection.Endpoint.Uri;
            otlpOptions.Protocol = connection.OtlpExportProtocol;
            otlpOptions.ExportProcessorType = connection.ExportProcessorType;

            if (connection.Endpoint.Headers != null)
            {
                otlpOptions.Headers = connection.Endpoint.Headers;
            }

            if (connection.Endpoint.BatchTimeoutInMs != null)
            {
                otlpOptions.TimeoutMilliseconds = connection.Endpoint.BatchTimeoutInMs.Value;
            }

            if (connection.ClientPrefix != null)
            {
                otlpOptions.UserAgentProductIdentifier = connection.ClientPrefix;
            }

            if (connection.Endpoint.EnableTls)
            {
                if (string.IsNullOrWhiteSpace(connection.Endpoint.ClientCertificatePath))
                {
                    throw new Exception("Tls is enabled for otel endpoint, but client certificate path is not set.");
                }
                otlpOptions.HttpClientFactory = () =>
                {
                    var handler = new HttpClientHandler();
                    var cert = X509CertificateLoader.LoadPkcs12FromFile(connection.Endpoint.ClientCertificatePath, connection.Endpoint.ClientCertificatePassword);

                    handler.ClientCertificates.Add(cert);
                    return new HttpClient(handler);
                };
            }

            return otlpOptions;
        }
    }
}
