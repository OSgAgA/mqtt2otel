using Microsoft.Extensions.Logging;
using Moq;
using mqtt2otel.Helper;
using mqtt2otel.InternalMetrics;
using mqtt2otel.Manifest;
using mqtt2otel.Metadata;
using mqtt2otel.Server.Helper;
using mqtt2otel.Tests.Helper;
using System;
using System.Collections.Generic;
using System.Text;

namespace mqtt2otel.Tests._30_SystemTests
{
    [Collection("MQTT Tests")]
    public class JsonTestCases
    {

        [Theory]
        [MemberData(nameof(TestCase.LoadAllAsMemberdata), MemberType = typeof(TestCase))]
        public async Task ShouldPassAllJsonTestCases(TestCase testCase)
        {
            // Arrange

            using var mqttHelper = new MqttTestHelper();
            await mqttHelper.EnsureServerIsStarted();

            var dataStores = GenericHelper.GetDataStores();
            var manifest = ManifestHelper.ReadManifestFromString(testCase.Setup.Manifest, dataStores);

            if (manifest.MqttConnections.Count == 0)
            {
                manifest.MqttConnections.Add(new MqttBroker());
            }

            foreach (var connection in manifest.MqttConnections)
            {
                connection.Endpoint.Port = 1883;
                connection.Endpoint.Address = "127.0.0.1";
                connection.Endpoint.ConnectionType = Manifest.MqttBrokerConnectionType.Tcp;
                connection.Endpoint.EnableTls = false;
            }
            manifest.Initialize();

            var loggerMockMqtt = new Mock<ILogger<MqttCoordinator>>();
            var mqttCoordinator = new MqttCoordinator(loggerMockMqtt.Object, new MqttMeter());
            var tcs = new TaskCompletionSource<MqttMessageReceivedEventArgs>();
            mqttCoordinator.OnMessageProcessed += (sender, args) => tcs.SetResult(args);

            await mqttCoordinator.ConnectAndSubscribe(manifest);

            var internalLogger = new Mock<ILogger<OtelCoordinator>>();
            var exportBuilder = new OtelTestExporterBuilder();
            var otelCoordinator = new OtelCoordinator(internalLogger.Object, exportBuilder, dataStores, new OtelMeter());
            otelCoordinator.Connect(manifest);

            // Act

            await mqttHelper.PublishPayload(testCase.Setup.Topic, testCase.Setup.Payload);

            var completedTask = await Task.WhenAny(
                               tcs.Task,
                               Task.Delay(1000, TestContext.Current.CancellationToken));

            Assert.True(completedTask == tcs.Task, "Callback was not triggered");

            otelCoordinator.FlushMeters();

            // Assert
            // Metrics

            Assert.Equal(testCase.ExpectedResult.Metrics.Count, exportBuilder.Metrics.Count);

            int i = 0;
            foreach (var expectedMetric in testCase.ExpectedResult.Metrics)
            {
                var metric = exportBuilder.Metrics[i++];

                Assert.Equal(expectedMetric.Name, metric.Name);
                Assert.Equal(expectedMetric.MetricType, metric.MetricType);
                Assert.Equal(expectedMetric.Unit, metric.Unit);
                Assert.Equal(expectedMetric.Description, metric.Description);

                int count = 0;
                foreach (var metricPoint in metric.GetMetricPoints())
                {
                    Assert.True(expectedMetric.MetricPoints.Count > count);
                    var expectedPoint = expectedMetric.MetricPoints[count++];
                    count++;
                    Assert.Equal(expectedPoint.Value.ToString(), metricPoint.GetValueAsObject(metric.MetricType).ToString());
                    Assert.Equal(expectedPoint.Tags.Count, metricPoint.Tags.Count);

                    foreach (var tag in metricPoint.Tags)
                    {
                        Assert.True(expectedPoint.Tags.ContainsKey(tag.Key));
                        Assert.Equal(expectedPoint.Tags[tag.Key]?.ToString(), tag.Value?.ToString());
                    }
                }

                Assert.Equal(expectedMetric.MetricPoints.Count+1, count);
            }

            // Logs

            Assert.Equal(testCase.ExpectedResult.Logs.Count, exportBuilder.Logs.Count);

            int logCount = 0;
            foreach (var expectedLogEntry in testCase.ExpectedResult.Logs)
            {
                Assert.True(exportBuilder.Logs.Count > logCount);
                var logEntry = exportBuilder.Logs[logCount++];
                Assert.Equal(expectedLogEntry.Body, logEntry.Body);
                Assert.Equal(expectedLogEntry.LogLevel, logEntry.LogLevel);
                Assert.Equal(expectedLogEntry.Timestamp, logEntry.Timestamp);
            }
        }
    }
}
