using Microsoft.Extensions.Logging;
using Moq;
using mqtt2otel.Helper;
using mqtt2otel.InternalMetrics;
using mqtt2otel.Parser;
using mqtt2otel.Server.Helper;
using mqtt2otel.Tests.Helper;
using System;
using System.Collections.Generic;
using System.Text;

namespace mqtt2otel.Tests._30_SystemTests
{
    [Collection("MQTT Tests")]
    public class SystemTests
    {
        [Fact]
        public async Task ShouldProcessSimpleMetric()
        {
            using var mqttHelper = new MqttTestHelper();
            await mqttHelper.EnsureServerIsStarted();
            await mqttHelper.ConnectClient();

            var yaml = """
                       Version: 1.0
                
                       MqttConnections:
                         - ClientPrefix: "mqtt2otel-dev"
                           Endpoint:
                             Port: 1883
                             Address: "127.0.0.1"
                             ConnectionType: Tcp
                             EnableTls: false

                       OtelConnections:
                         - Name: "TestConnection"
                           ServiceName: "TestServiceName"
                           Endpoint:
                             Port: 4711
                             Address: "1.2.3.4"
                             EnableTls: false

                       Processors:
                       - Name: "Test processor"
                         Mqtt:
                           Subscriptions:
                            - Name: "Temperature"
                              Topic: "sensors/temperature"
                         Otel:
                           Metrics:
                             - Name: "TestMetric"
                               Description: "Test metric description"
                               Attributes:
                                 - Key: "TestAttribute"
                                   Value: "TestValue"
                               Unit: "C"
                               SignalDataType: Double
                               Instrument: Gauge
                               Value: "JSONPATH('$.Temperature')"
                       """;

            var payloadParser = new PayloadParser();
            var embeddedExpressionParser = new EmbeddedExpressionParser(payloadParser);

            var dataStores = GenericHelper.GetDataStores(payloadParser, embeddedExpressionParser);
            var manifest = ManifestHelper.ReadManifestFromString(yaml, dataStores);
            payloadParser.SetMappings(manifest.Mappings);
            embeddedExpressionParser.SetMappings(manifest.Mappings);

            manifest.Initialize();

            var loggerMockMqtt = new Mock<ILogger<MqttCoordinator>>();
            var mqttCoordinator = new MqttCoordinator(loggerMockMqtt.Object, new MqttMeter());

            try
            {
                var tcs = new TaskCompletionSource<MqttMessageReceivedEventArgs>();
                mqttCoordinator.OnMessageProcessed += (sender, args) => tcs.SetResult(args);

                await mqttCoordinator.ConnectAndSubscribe(manifest);

                var internalLogger = new Mock<ILogger<OtelCoordinator>>();
                var exportBuilder = new OtelTestExporterBuilder();

                using (var otelCoordinator = new OtelCoordinator(internalLogger.Object, exportBuilder, dataStores, new OtelInternalMeter(), embeddedExpressionParser, new ApplicationSettings()))
                {
                    otelCoordinator.Connect(manifest);

                    string topic = "sensors/temperature";
                    string payload = "{ Temperature: 42 }";
                    await mqttHelper.PublishPayload(topic, payload);

                    var completedTask = await Task.WhenAny(
                                       tcs.Task,
                                       Task.Delay(1000, TestContext.Current.CancellationToken));

                    Assert.True(completedTask == tcs.Task, "Callback was not triggered");
                }

                Assert.Single(exportBuilder.GetAllMetrics());
                var metric = exportBuilder.GetAllMetrics().First().Value;
                Assert.Equal("TestMetric", metric.Name);
                Assert.Equal("DoubleGauge", metric.MetricType.ToString());
                Assert.Equal("C", metric.Unit);
                Assert.Equal("Test metric description", metric.Description);

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
            finally
            {
                await mqttCoordinator.DisconnectAllBrokers();
                await mqttHelper.DisconnectClient();
            }
        }

        [Fact]
        public async Task ShouldProcessSimpleLogEntry()
        {
            using var mqttHelper = new MqttTestHelper();
            await mqttHelper.EnsureServerIsStarted();
            await mqttHelper.ConnectClient();

            var yaml = """
                       Version: 1.0
                
                       MqttConnections:
                         - ClientPrefix: "mqtt2otel-dev"
                           Endpoint:
                             Port: 1883
                             Address: "127.0.0.1"
                             ConnectionType: Tcp
                             EnableTls: false

                       OtelConnections:
                         - Name: "TestConnection"
                           ServiceName: "TestServiceName"
                           Endpoint:
                             Port: 4711
                             Address: "1.2.3.4"
                             EnableTls: false

                       Processors:
                       - Name: "Test processor"
                         Mqtt:
                           Subscriptions:
                            - Name: "Temperature"
                              Topic: "sensors/logEntry"
                         Otel:
                           Logs:
                             -  Name: "Test_Log"
                                Description: "A simple log message rule."
                                Transform: "GROK('%{TIMESTAMP_ISO8601:otel_timestamp} %{WORD:otel_loglevel} %{GREEDYDATA:otel_message}')"
                                PayloadType: Json
                       """;

            var payloadParser = new PayloadParser();
            var embeddedExpressionParser = new EmbeddedExpressionParser(payloadParser);

            var dataStores = GenericHelper.GetDataStores(payloadParser, embeddedExpressionParser);
            var manifest = ManifestHelper.ReadManifestFromString(yaml, dataStores);
            payloadParser.SetMappings(manifest.Mappings);
            embeddedExpressionParser.SetMappings(manifest.Mappings);
            manifest.Initialize();

            var loggerMockMqtt = new Mock<ILogger<MqttCoordinator>>();
            var mqttCoordinator = new MqttCoordinator(loggerMockMqtt.Object, new MqttMeter());

            try
            {
                var tcs = new TaskCompletionSource<MqttMessageReceivedEventArgs>();
                mqttCoordinator.OnMessageProcessed += (sender, args) => tcs.SetResult(args);

                await mqttCoordinator.ConnectAndSubscribe(manifest);

                var internalLogger = new Mock<ILogger<OtelCoordinator>>();
                var exportBuilder = new OtelTestExporterBuilder();

                using (var otelCoordinator = new OtelCoordinator(internalLogger.Object, exportBuilder, dataStores, new OtelInternalMeter(), embeddedExpressionParser, new ApplicationSettings()))
                {
                    otelCoordinator.Connect(manifest);

                    string topic = "sensors/logEntry";
                    string payload = "2026-01-31T15:42Z WARN This is a simple log message.";
                    await mqttHelper.PublishPayload(topic, payload);

                    var completedTask = await Task.WhenAny(
                                       tcs.Task,
                                       Task.Delay(100, TestContext.Current.CancellationToken));

                    Assert.True(completedTask == tcs.Task, "Callback was not triggered");
                }

                Assert.Single(exportBuilder.GetAllLogs());
                var logEntry = exportBuilder.GetAllLogs().First().Value;
                Assert.Equal("This is a simple log message.", logEntry.Body);
                Assert.Equal(LogLevel.Warning, logEntry.LogLevel);
                Assert.Equal(new DateTime(2026, 1, 31, 15, 42, 0), logEntry.Timestamp);
            }
            finally
            {
                await mqttCoordinator.DisconnectAllBrokers();
                await mqttHelper.DisconnectClient();
            }
        }
    }
}
