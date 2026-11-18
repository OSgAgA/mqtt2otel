using mqtt2otel.Manifest;
using mqtt2otel.Shared.Helper;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace mqtt2otel
{
    /// <summary>
    /// Responsible for creating open telemetry collector configs that are used internally.
    /// </summary>
    public static class OtelCollectorConfig
    {
        /// <summary>
        /// Defines how much spaces are used to indent content by one level.
        /// </summary>
        private const int indentLevel = 2;

        /// <summary>
        /// The name of the routing receiver inside the open telemetry collector config.
        /// </summary>
        private const string routingReceiverName = "routing";

        /// <summary>
        /// The name of the transform processor for traces.
        /// </summary>
        private const string traceTransformProcessorName = "transform/traces";

        /// <summary>
        /// The name of the transform processor for traces.
        /// </summary>
        private const string logTransformProcessorName = "transform/logs";

        /// <summary>
        /// The name of the transform processor for traces.
        /// </summary>
        private const string metricTransformProcessorName = "transform/metrics";

        /// <summary>
        /// The name of the resource processor that strips internal attributes.
        /// </summary>
        private const string resourceStripInternalProcessorName = "resource/strip_internal";

        /// <summary>
        /// Gets the address of the collector endpoint.
        /// </summary>
        private const string collectorEndpointAddress = "0.0.0.0:4317";

        /// <summary>
        /// Returns a prefix string that creates an indentation matchint the provided level.
        /// </summary>
        /// <param name="level">The indentation level.</param>
        /// <returns>The generated prefix indentation string.</returns>
        private static string indent(int level)
        {
            return new string(' ', indentLevel * level);
        }

        /// <summary>
        /// Get the name of the exporter for the provided connection.
        /// </summary>
        /// <param name="connection">The otel server connection data.</param>
        /// <returns>The exporter name.</returns>
        private static string GetExporterName(OtelServerConnection connection)
        {
            if (connection.OtlpExportProtocol == OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf)
            {
                return $"otlp_http/{connection.Slug}";
            }

            return $"otlp_grpc/{connection.Slug}";
        }        

        /// <summary>
        /// Create and store the collector config.
        /// </summary>
        /// <param name="connections">The available connections.</param>
        public static string CreateConfig(List<OtelServerConnection> connections)
        {
            var sb = new StringBuilder();

            CreateReceiver(sb);
            sb.AppendLine();
            CreateProcessors(sb);
            sb.AppendLine();
            CreateExporters(sb, connections);
            sb.AppendLine();
            CreateRoutingConnector(sb, connections);
            sb.AppendLine();
            CreateService(sb, connections);

            return sb.ToString();

        }

        /// <summary>
        /// Create the config receiver section.
        /// </summary>
        /// <param name="sb">The string builder.</param>
        /// <param name="connections">The connections.</param>
        private static void CreateReceiver(StringBuilder sb)
        {
            sb.AppendLine("receivers:");
            sb.AppendLine($"{indent(1)}otlp:");
            sb.AppendLine($"{indent(2)}protocols:");
            sb.AppendLine($"{indent(3)}grpc:");
            sb.AppendLine($"{indent(4)}endpoint: {collectorEndpointAddress}");
        }

        /// <summary>
        /// Create the config trace processor section.
        /// </summary>
        /// <param name="sb">The string builder.</param>
        /// <param name="connections">The connections.</param>
        private static void CreateProcessors(StringBuilder sb)
        {
            sb.AppendLine("processors:");
            CreateTraceProcessor(sb);
            sb.AppendLine();
            CreateLogProcessor(sb);
            sb.AppendLine();
            CreateMetricProcessor(sb);
            sb.AppendLine();
            CreateResoureceStripInternalProcessor(sb);
        }

        /// <summary>
        /// Create the config trace processor section.
        /// </summary>
        /// <param name="sb">The string builder.</param>
        /// <param name="connections">The connections.</param>
        private static void CreateTraceProcessor(StringBuilder sb)
        {
            string spanIdAttribute = $"\"{InternalAttributes.SpanId}\"";
            string traceIdAttribute = $"\"{InternalAttributes.TraceId}\"";

            sb.AppendLine($"{indent(1)}{traceTransformProcessorName}:");
            sb.AppendLine($"{indent(2)}error_mode: ignore          # a failing statement skips that span instead of dropping the batch");
            sb.AppendLine($"{indent(2)}trace_statements:");
            sb.AppendLine($"{indent(3)}- context: span");
            sb.AppendLine($"{indent(3)}  statements:");

            sb.AppendLine($"{indent(4)}  - set(span.span_id, SpanID(span.attributes[{spanIdAttribute}])) where span.attributes[{spanIdAttribute}] != nil");
            sb.AppendLine($"{indent(4)}  - set(span.trace_id, TraceID(span.attributes[{traceIdAttribute}])) where span.attributes[{traceIdAttribute}] != nil");
            sb.AppendLine($"{indent(4)}  - delete_key(span.attributes, {spanIdAttribute})");
            sb.AppendLine($"{indent(4)}  - delete_key(span.attributes, {traceIdAttribute})");
        }

        /// <summary>
        /// Create the config log processor section.
        /// </summary>
        /// <param name="sb">The string builder.</param>
        /// <param name="connections">The connections.</param>
        private static void CreateLogProcessor(StringBuilder sb)
        {
            string spanIdAttribute = $"\"{InternalAttributes.SpanId}\"";
            string traceIdAttribute = $"\"{InternalAttributes.TraceId}\"";

            sb.AppendLine($"{indent(1)}{logTransformProcessorName}:");
            sb.AppendLine($"{indent(2)}error_mode: ignore          # a failing statement skips that log instead of dropping the batch");
            sb.AppendLine($"{indent(2)}log_statements:");
            sb.AppendLine($"{indent(3)}- context: log");
            sb.AppendLine($"{indent(3)}  statements:");

            sb.AppendLine($"{indent(4)}  - set(log.span_id, SpanID(log.attributes[{spanIdAttribute}])) where log.attributes[{spanIdAttribute}] != nil");
            sb.AppendLine($"{indent(4)}  - set(log.trace_id, TraceID(log.attributes[{traceIdAttribute}])) where log.attributes[{traceIdAttribute}] != nil");
            sb.AppendLine($"{indent(4)}  - delete_key(log.attributes, {spanIdAttribute})");
            sb.AppendLine($"{indent(4)}  - delete_key(log.attributes, {traceIdAttribute})");
        }

        /// <summary>
        /// Create the config metric processor section.
        /// </summary>
        /// <param name="sb">The string builder.</param>
        /// <param name="connections">The connections.</param>
        private static void CreateMetricProcessor(StringBuilder sb)
        {
            string timespanAttribute = $"\"{InternalAttributes.TimespanId}\"";

            sb.AppendLine($"{indent(1)}{metricTransformProcessorName}:");
            sb.AppendLine($"{indent(2)}error_mode: ignore          # a failing statement skips that log instead of dropping the batch");
            sb.AppendLine($"{indent(2)}metric_statements:");
            sb.AppendLine($"{indent(3)}- context: datapoint");
            sb.AppendLine($"{indent(3)}  statements:");

            sb.AppendLine($"{indent(4)}  - set(datapoint.time_unix_nano, Int(datapoint.attributes[{timespanAttribute}]) * 100) where datapoint.attributes[{timespanAttribute}] != nil     # just a test. Do not use --> Cardinality issue at .net sdk.");
            sb.AppendLine($"{indent(4)}  - delete_key(datapoint.attributes, {timespanAttribute})");
        }

        /// <summary>
        /// Create the config trace processor section.
        /// </summary>
        /// <param name="sb">The string builder.</param>
        /// <param name="connections">The connections.</param>
        private static void CreateResoureceStripInternalProcessor(StringBuilder sb)
        {
            sb.AppendLine($"{indent(1)}{resourceStripInternalProcessorName}:");
            sb.AppendLine($"{indent(2)}attributes:");
            sb.AppendLine($"{indent(3)}- key: {InternalAttributes.ExporterRoutingId}");
            sb.AppendLine($"{indent(3)}  action: delete");
        }

        /// <summary>
        /// Create the config exporters section.
        /// </summary>
        /// <param name="sb">The string builder.</param>
        /// <param name="connections">The connections.</param>

        private static void CreateExporters(StringBuilder sb, List<OtelServerConnection> connections)
        {
            if (connections.Count == 0) return;

            sb.AppendLine("exporters:");

            sb.AppendLine($"{indent(1)}debug:");
            sb.AppendLine($"{indent(2)}verbosity: detailed");
            sb.AppendLine();

            foreach (var connection in connections)
            {
                if (connection.OtlpExportProtocol == OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf)
                {
                    CreateTraceHttpProtobufExporter(sb, connection);
                }
                else
                {
                    CreateTraceGrpcExporter(sb, connection);

                }
            }
        }

        /// <summary>
        /// Create the config grpc exporters.
        /// </summary>
        /// <param name="sb">The string builder.</param>
        /// <param name="connection">The connection.</param>

        private static void CreateTraceGrpcExporter(StringBuilder sb, OtelServerConnection connection)
        {
            sb.AppendLine($"{indent(1)}{GetExporterName(connection)}:");
            sb.AppendLine($"{indent(2)}endpoint: {connection.Endpoint.FullAddress.Trim().Replace("\"", "").ReplaceLineEndings("")}");
            sb.AppendLine($"{indent(2)}sending_queue:");
            if (connection.ExportProcessorType == OpenTelemetry.ExportProcessorType.Batch)
            {
                if (connection.Endpoint.BatchTimeoutInMs == null)
                {
                    sb.AppendLine($"{indent(3)}batch: {{}}");
                }
                else
                {
                    sb.AppendLine($"{indent(3)}batch: ");
                    sb.AppendLine($"{indent(4)}flush_timeout: {connection.Endpoint.BatchTimeoutInMs}");
                }
            }

            if (connection.Endpoint.EnableTls)
            {
                sb.AppendLine($"{indent(2)}tls:");
                sb.AppendLine($"{indent(3)}cert_file: {connection.Endpoint.ClientCertificatePath}");
                sb.AppendLine($"{indent(3)}key_file: {connection.Endpoint.ClientCertificatePassword}");
            }
        }

        /// <summary>
        /// Create the config http protobuf exporters.
        /// </summary>
        /// <param name="sb">The string builder.</param>
        /// <param name="connection">The connection.</param>

        private static void CreateTraceHttpProtobufExporter(StringBuilder sb, OtelServerConnection connection)
        {
            sb.AppendLine($"{indent(1)}{GetExporterName(connection)}:");
            sb.AppendLine($"{indent(2)}endpoint: {connection.Endpoint.FullAddress.Trim().Replace("\"", "").ReplaceLineEndings("")}");
            sb.AppendLine($"{indent(2)}sending_queue:");
            if (connection.ExportProcessorType == OpenTelemetry.ExportProcessorType.Batch)
            {
                if (connection.Endpoint.BatchTimeoutInMs == null)
                {
                    sb.AppendLine($"{indent(3)}batch: {{}}");
                }
                else
                {
                    sb.AppendLine($"{indent(3)}batch: ");
                    sb.AppendLine($"{indent(4)}flush_timeout: {connection.Endpoint.BatchTimeoutInMs}");
                }
            }

            if (!string.IsNullOrWhiteSpace(connection.Endpoint.Headers))
            {
                var headers = connection.Endpoint.Headers.Split(',');
                var headerDict = new Dictionary<string, string>();

                foreach (var h in headers)
                {
                    var parts = h.Split(new[] { ':', '=' }, 2);

                    if (parts == null || parts.Length != 2) continue;

                    headerDict[parts[0].Trim().Replace("\"", "").ReplaceLineEndings("")] = parts[1].Trim().Replace("\"", "").ReplaceLineEndings("");
                }

                if (headerDict.Any())
                {
                    sb.AppendLine($"{indent(2)}headers:");

                    foreach (var kv in headerDict)
                    {
                        sb.AppendLine($"{indent(3)}{kv.Key}: \"{kv.Value}\"");
                    }
                }
            }

            if (connection.Endpoint.EnableTls)
            {
                sb.AppendLine($"{indent(2)}tls:");
                sb.AppendLine($"{indent(3)}cert_file: {connection.Endpoint.ClientCertificatePath}");
                sb.AppendLine($"{indent(3)}key_file: {connection.Endpoint.ClientCertificatePassword}");
            }
        }

        /// <summary>
        /// Create the config routing section.
        /// </summary>
        /// <param name="sb">The string builder.</param>
        /// <param name="connections">The connections.</param>

        private static void CreateRoutingConnector(StringBuilder sb, List<OtelServerConnection> connections)
        {
            if (connections.Count == 0) return;

            sb.AppendLine("connectors:");
            sb.AppendLine($"{indent(1)}{routingReceiverName}/traces:");
            sb.AppendLine($"{indent(2)}table:");

            foreach (OtelServerConnection connection in connections)
            {
                sb.AppendLine($"{indent(3)}- condition: resource.attributes[\"{InternalAttributes.ExporterRoutingId}\"] == \"{connection.Slug}\"");
                sb.AppendLine($"{indent(3)}  pipelines: [traces/{connection.Slug}]");
            }

            sb.AppendLine();
            sb.AppendLine($"{indent(1)}{routingReceiverName}/logs:");
            sb.AppendLine($"{indent(2)}table:");

            foreach (OtelServerConnection connection in connections)
            {
                sb.AppendLine($"{indent(3)}- condition: resource.attributes[\"{InternalAttributes.ExporterRoutingId}\"] == \"{connection.Slug}\"");
                sb.AppendLine($"{indent(3)}  pipelines: [logs/{connection.Slug}]");
            }

            sb.AppendLine();
            sb.AppendLine($"{indent(1)}{routingReceiverName}/metrics:");
            sb.AppendLine($"{indent(2)}table:");

            foreach (OtelServerConnection connection in connections)
            {
                sb.AppendLine($"{indent(3)}- condition: resource.attributes[\"{InternalAttributes.ExporterRoutingId}\"] == \"{connection.Slug}\"");
                sb.AppendLine($"{indent(3)}  pipelines: [metrics/{connection.Slug}]");
            }

        }

        /// <summary>
        /// Create the config service section.
        /// </summary>
        /// <param name="sb">The string builder.</param>
        /// <param name="connections">The connections.</param>
        private static void CreateService(StringBuilder sb, List<OtelServerConnection> connections)
        {
            if (connections.Count == 0) return;

            sb.AppendLine("service:");
            sb.AppendLine($"{indent(1)}pipelines:");
            sb.AppendLine($"{indent(2)}traces/in:");
            sb.AppendLine($"{indent(3)}receivers: [otlp]");
            sb.AppendLine($"{indent(3)}exporters: [routing/traces]");
            sb.AppendLine();

            sb.AppendLine($"{indent(2)}logs/in:");
            sb.AppendLine($"{indent(3)}receivers: [otlp]");
            sb.AppendLine($"{indent(3)}exporters: [routing/logs]");
            sb.AppendLine();

            sb.AppendLine($"{indent(2)}metrics/in:");
            sb.AppendLine($"{indent(3)}receivers: [otlp]");
            sb.AppendLine($"{indent(3)}exporters: [routing/metrics]");

            foreach (var connection in connections)
            {
                sb.AppendLine();
                sb.AppendLine($"{indent(2)}traces/{connection.Slug}:");
                sb.AppendLine($"{indent(3)}receivers: [{routingReceiverName}/traces]");
                sb.AppendLine($"{indent(3)}processors: [{traceTransformProcessorName}, {resourceStripInternalProcessorName}]");
                sb.AppendLine($"{indent(3)}exporters: [{GetExporterName(connection)}]");
                sb.AppendLine();

                sb.AppendLine($"{indent(2)}logs/{connection.Slug}:");
                sb.AppendLine($"{indent(3)}receivers: [{routingReceiverName}/logs]");
                sb.AppendLine($"{indent(3)}processors: [{logTransformProcessorName}, {resourceStripInternalProcessorName}]");
                sb.AppendLine($"{indent(3)}exporters: [{GetExporterName(connection)}]");
                sb.AppendLine();

                sb.AppendLine($"{indent(2)}metrics/{connection.Slug}:");
                sb.AppendLine($"{indent(3)}receivers: [{routingReceiverName}/metrics]");
                sb.AppendLine($"{indent(3)}processors: [{metricTransformProcessorName}, {resourceStripInternalProcessorName}]");
                sb.AppendLine($"{indent(3)}exporters: [{GetExporterName(connection)}]");
            }
        }
    }
}
