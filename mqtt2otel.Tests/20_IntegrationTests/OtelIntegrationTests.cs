using Microsoft.Extensions.Logging;
using Moq;
using mqtt2otel.Helper;
using mqtt2otel.InternalMetrics;
using mqtt2otel.Tests.Helper;
using System;
using System.Collections.Generic;
using System.Text;

namespace mqtt2otel.Tests._20_IntegrationTests
{
    public class OtelIntegrationTests
    {
        [Fact]
        public async Task ShouldReceiveMetric()
        {
            var yaml = """
                       Version: 1.0
                
                       OtelConnections:
                         - Name: "Local"
                           ServiceName: "mqtt2otel"
                           ServiceNamespace: "dev-mqtt2otel"
                           ExportProcessorType: "Simple"
                           OtlpExportProtocol: "Grpc"
                           Endpoint:
                             Protocol: "http"
                             Port: 32014
                             Address: "192.168.1.9"
                             EnableTls: false

                       Processors:
                       - Name: "Test processor"
                         Mqtt:
                           Subscriptions:
                            - Name: "Temperature"
                              Topic: "sensors/temperature"
                         Otel:
                           Metrics:
                             -  Name: "Test_Metric"
                                Description: "The current power consumption at the time of measurement in Watt."
                                Attributes:
                                  - Key: "TestAttribute"
                                    Value: "TestValue"
                                SignalDataType: Double
                                Instrument: Gauge
                                Unit: "W"
                                Value: "PAYLOAD()"
                       """;

            var manifest = ManifestHelper.ReadManifestFromString(yaml);
            manifest.Initialize();

            var internalLogger = new Mock<ILogger<OtelCoordinator>>();
            var dataStores = GenericHelper.GetDataStores();
            var exportBuilder = new OtelTestExporterBuilder();
            var otelCoordinator = new OtelCoordinator(internalLogger.Object, exportBuilder, dataStores, new OtelMeter());
            otelCoordinator.Connect(manifest);

            var subscription = manifest.Processors[0].Mqtt.Subscriptions[0];
            var rule = manifest.Processors[0].Otel.Metrics[0];
            GenericHelper.WriteMetricToSignalStore<double>(subscription, rule, dataStores.SignalStore, 42.0);

            otelCoordinator.FlushMeters();

            Assert.Single(exportBuilder.Metrics);
            var metric = exportBuilder.Metrics[0];
            Assert.Equal("Test_Metric", metric.Name);
            Assert.Equal("DoubleGauge", metric.MetricType.ToString());
            Assert.Equal("W", metric.Unit);
            Assert.Equal("The current power consumption at the time of measurement in Watt.", metric.Description);

            int count = 0;
            foreach (var metricPoint in metric.GetMetricPoints())
            {
                count++;
                Assert.Equal(42.0, metricPoint.GetGaugeLastValueDouble());
                Assert.Equal(1, metricPoint.Tags.Count);
                var enumerator = metricPoint.Tags.GetEnumerator();
                enumerator.MoveNext();
                var keyValue = enumerator.Current;
                Assert.Equal("TestAttribute", keyValue.Key);
                Assert.Equal("TestValue", keyValue.Value);
            }

            Assert.Equal(1, count);
        }

        [Fact]
        public async Task ShouldReceiveLogEntry()
        {
            var yaml = """
                       Version: 1.0
                
                       OtelConnections:
                         - Name: "Local"
                           ServiceName: "mqtt2otel"
                           ServiceNamespace: "dev-mqtt2otel"
                           ExportProcessorType: "Simple"
                           OtlpExportProtocol: "Grpc"
                           Endpoint:
                             Protocol: "http"
                             Port: 32014
                             Address: "192.168.1.9"
                             EnableTls: false

                       Processors:
                       - Name: "Test processor"
                         Mqtt:
                           Subscriptions:
                            - Name: "Temperature"
                              Topic: "sensors/temperature"
                         Otel:
                           Logs:
                             -  Name: "Test_Log"
                                Description: "A simple log message rule."
                                PayloadType: Text
                       """;

            var manifest = ManifestHelper.ReadManifestFromString(yaml);
            manifest.Initialize();

            var internalLogger = new Mock<ILogger<OtelCoordinator>>();
            var dataStores = GenericHelper.GetDataStores();
            var exportBuilder = new OtelTestExporterBuilder();
            var otelCoordinator = new OtelCoordinator(internalLogger.Object, exportBuilder, dataStores, new OtelMeter());
            otelCoordinator.Connect(manifest);

            var logRule = manifest.Processors[0].Otel.Logs[0];

            var logger = dataStores.LoggerStore.GetLogger(logRule.Id);
            bool success = await logger.ProcessLogMessage("This is a test message", logRule, new List<Variable>(), internalLogger.Object, logRule.Attributes);

            Assert.Single(exportBuilder.Logs);
            var logEntry = exportBuilder.Logs[0];
            Assert.Equal("This is a test message", logEntry.Body);
        }

        [Fact]
        public async Task ShouldReceiveTransformedLogEntryWithCorrectTimestampAndLogLevel()
        {
            var yaml = """
                       Version: 1.0
                
                       OtelConnections:
                         - Name: "Local"
                           ServiceName: "mqtt2otel"
                           ServiceNamespace: "dev-mqtt2otel"
                           ExportProcessorType: "Simple"
                           OtlpExportProtocol: "Grpc"
                           Endpoint:
                             Protocol: "http"
                             Port: 32014
                             Address: "192.168.1.9"
                             EnableTls: false

                       Processors:
                       - Name: "Test processor"
                         Mqtt:
                           Subscriptions:
                            - Name: "Temperature"
                              Topic: "sensors/temperature"
                         Otel:
                           Logs:
                             -  Name: "Test_Log"
                                Description: "A simple log message rule."
                                Transform: "GROK('%{TIMESTAMP_ISO8601:otel_timestamp} %{WORD:otel_loglevel} %{GREEDYDATA:otel_message}')"
                                PayloadType: Json
                       """;

            var manifest = ManifestHelper.ReadManifestFromString(yaml);
            manifest.Initialize();

            var internalLogger = new Mock<ILogger<OtelCoordinator>>();
            var dataStores = GenericHelper.GetDataStores();
            var exportBuilder = new OtelTestExporterBuilder();
            var otelCoordinator = new OtelCoordinator(internalLogger.Object, exportBuilder, dataStores, new OtelMeter());
            otelCoordinator.Connect(manifest);

            var logRule = manifest.Processors[0].Otel.Logs[0];

            var logger = dataStores.LoggerStore.GetLogger(logRule.Id);
            bool success = await logger.ProcessLogMessage("2026-01-31T15:42Z WARN This is a simple log message.", logRule, new List<Variable>(), internalLogger.Object, logRule.Attributes);

            Assert.Single(exportBuilder.Logs);
            var logEntry = exportBuilder.Logs[0];
            Assert.Equal("This is a simple log message.", logEntry.Body);
            Assert.Equal(LogLevel.Warning, logEntry.LogLevel);
            Assert.Equal(new DateTime(2026, 1, 31, 15, 42, 0), logEntry.Timestamp);
        }
    }
}
