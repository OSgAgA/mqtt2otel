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
        /// A regular expression that checks whether a character is invalid for a otel collector name.
        /// </summary>
        private static readonly Regex InvalidCharsForCollectorNames = new("[^a-z0-9_-]+", RegexOptions.Compiled);

        /// <summary>
        /// Get the name of the exporter for the provided connection.
        /// </summary>
        /// <param name="connection">The otel server connection data.</param>
        /// <returns>The exporter name.</returns>
        private static string GetExporterName(OtelServerConnection connection)
        {
            if (connection.OtlpExportProtocol == OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf)
            {
                return $"otlp_http/{CreateSlug(connection.Name)}";
            }

            return $"otlp_grpc/{CreateSlug(connection.Name)}";
        }
        /// <summary>
        /// Converts a string into a valid component name.
        /// </summary>
        /// <param name="input">The input string.</param>
        /// <param name="maxLength">Maximum amount of allowed characters.</param>
        /// <returns>The created component name.</returns>
        public static string CreateSlug(string input, int maxLength = 64)
        {
            // 1. normalize: lower case, replace every run of invalid characters with a single underscore
            var slug = InvalidCharsForCollectorNames.Replace(input.Trim().ToLowerInvariant(), "_").Trim('_', '-');
            if (slug.Length == 0) slug = "x";

            // 2. add a short hash of the ORIGINAL string, so different inputs never collapse into the same name
            var hash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(input)))[..8];

            // 3. cut the slug so slug + "_" + hash fits
            var max = maxLength - hash.Length - 1;
            if (slug.Length > max) slug = slug[..max].TrimEnd('_', '-');

            return $"{slug}_{hash}";
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
        /// Create the config trace processor section.
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
                sb.AppendLine($"{indent(3)}- condition: resource.attributes[\"{InternalAttributes.ExporterRoutingId}\"] == \"{CreateSlug(connection.Name)}\"");
                sb.AppendLine($"{indent(3)}  pipelines: [traces/{CreateSlug(connection.Name)}]");
            }

            sb.AppendLine();
            sb.AppendLine($"{indent(1)}{routingReceiverName}/logs:");
            sb.AppendLine($"{indent(2)}table:");

            foreach (OtelServerConnection connection in connections)
            {
                sb.AppendLine($"{indent(3)}- condition: resource.attributes[\"{InternalAttributes.ExporterRoutingId}\"] == \"{CreateSlug(connection.Name)}\"");
                sb.AppendLine($"{indent(3)}  pipelines: [logs/{CreateSlug(connection.Name)}]");
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

            foreach (var connection in connections)
            {
                sb.AppendLine();
                sb.AppendLine($"{indent(2)}traces/{CreateSlug(connection.Name)}:");
                sb.AppendLine($"{indent(3)}receivers: [{routingReceiverName}/traces]");
                sb.AppendLine($"{indent(3)}processors: [{traceTransformProcessorName}, {resourceStripInternalProcessorName}]");
                sb.AppendLine($"{indent(3)}exporters: [{GetExporterName(connection)}]");
                sb.AppendLine();

                sb.AppendLine($"{indent(2)}logs/{CreateSlug(connection.Name)}:");
                sb.AppendLine($"{indent(3)}receivers: [{routingReceiverName}/logs]");
                sb.AppendLine($"{indent(3)}processors: [{logTransformProcessorName}, {resourceStripInternalProcessorName}]");
                sb.AppendLine($"{indent(3)}exporters: [{GetExporterName(connection)}]");
            }

        }
    }
}
