using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.Utilities;
using Moq;
using mqtt2otel.Helper;
using mqtt2otel.InternalMetrics;
using mqtt2otel.Manifest;
using mqtt2otel.Metadata;
using mqtt2otel.Server.Helper;
using mqtt2otel.Tests.Helper;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Xunit.Sdk;

namespace mqtt2otel.Tests._30_SystemTests
{
    [Collection("MQTT Tests")]
    public class JsonTestCases
    {
        private readonly ITestOutputHelper _output;

        public JsonTestCases(ITestOutputHelper output)
        {
            _output = output;
        }

        [Theory]
        [MemberData(nameof(TestCaseData.LoadAllAsMemberdata), MemberType = typeof(TestCaseData))]
        public async Task ShouldPassAllJsonTestCases(TestCaseData testCase)
        {
            var culture = new CultureInfo("en-US");
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;

            this._output.WriteLine($"{DateTime.UtcNow}: Executing test case with id: '{testCase.Setup.Id}'");

            // Arrange

            using var mqttHelper = new MqttTestHelper();
            await mqttHelper.EnsureServerIsStarted();

            var dataStores = GenericHelper.GetDataStores();
            var manifest = ManifestHelper.ReadManifestFromString(testCase.Setup.Manifest, dataStores);

            if (string.IsNullOrWhiteSpace(manifest.Version)) manifest.Version = "1.0";

            if (manifest.MqttConnections.Count == 0)
            {
                manifest.MqttConnections.Add(new MqttBroker());
            }

            if (manifest.OtelConnections.Count == 0)
            {
                manifest.OtelConnections.Add(new OtelServerConnection());
            }

            foreach (var connection in manifest.MqttConnections)
            {
                connection.Endpoint.Port = 1883;
                connection.Endpoint.Address = "127.0.0.1";
                connection.Endpoint.ConnectionType = Manifest.MqttBrokerConnectionType.Tcp;
                connection.Endpoint.EnableTls = false;
            }
            manifest.Initialize();

            // Skip all further test, if only manifest should be validated.
            if (testCase.Setup.ValidateManifestOnly) return;

            var loggerMockMqtt = new Mock<ILogger<MqttCoordinator>>();
            var mqttCoordinator = new MqttCoordinator(loggerMockMqtt.Object, new MqttMeter());
            var tcs = new TaskCompletionSource<MqttMessageReceivedEventArgs>();
            int tcsCount = 1;
            int expectedCount = testCase.ExpectedResult.Metrics.Count + testCase.ExpectedResult.Logs.Count;
            mqttCoordinator.OnMessageProcessed += (sender, args) =>
            {
                if (expectedCount == tcsCount++) tcs.SetResult(args);
            };

            await mqttCoordinator.ConnectAndSubscribe(manifest);

            var internalLogger = new Mock<ILogger<OtelCoordinator>>();
            var exportBuilder = new OtelTestExporterBuilder();
            var otelCoordinator = new OtelCoordinator(internalLogger.Object, exportBuilder, dataStores, new OtelMeter());
            otelCoordinator.Connect(manifest);

            this._output.WriteLine($"{DateTime.UtcNow}: Arrange completed.");

            // Act

            await mqttHelper.PublishPayload(testCase.Setup.Topic, testCase.Setup.Payload);

            var completedTask = await Task.WhenAny(
                               tcs.Task,
                               Task.Delay(1000, TestContext.Current.CancellationToken));

            Assert.True(completedTask == tcs.Task, "Callback was not triggered");

            otelCoordinator.FlushMeters();

            this._output.WriteLine($"{DateTime.UtcNow}: Act completed.");

            // Assert
            // Metrics

            AssertEqual(testCase.ExpectedResult.Metrics.Count, exportBuilder.Metrics.Count, "metrics.count");

            int i = 0;
            foreach (var expectedMetric in testCase.ExpectedResult.Metrics)
            {
                var metric = exportBuilder.Metrics[i++];

                AssertEqual(expectedMetric.Name, metric.Name, "Metric.Name");
                AssertEqual(expectedMetric.MetricType, metric.MetricType, "Metric.Type");
                AssertEqual(expectedMetric.Unit, metric.Unit, "Metric.Unit");
                AssertEqual(expectedMetric.Description, metric.Description, "Metric.Description");

                int count = 0;
                foreach (var metricPoint in metric.GetMetricPoints())
                {
                    Assert.True(expectedMetric.MetricPoints.Count > count);
                    var expectedPoint = expectedMetric.MetricPoints[count++];
                    count++;
                    AssertEqual(expectedPoint.Value.ToString(), metricPoint.GetValueAsObject(metric.MetricType).ToString(), "MetricPoint.Value");
                    AssertEqual(expectedPoint.Tags.Count, metricPoint.Tags.Count, "tags.count");

                    foreach (var tag in metricPoint.Tags)
                    {
                        Assert.True(expectedPoint.Tags.ContainsKey(tag.Key));
                        AssertEqual(expectedPoint.Tags[tag.Key]?.ToString(), tag.Value?.ToString(), $"Tag.Value for key {tag.Key}");
                    }
                }

                AssertEqual(expectedMetric.MetricPoints.Count + 1, count, "metricPoints.Count+1");
            }

            this._output.WriteLine($"{DateTime.UtcNow}: Assert metrics completed.");

            // Logs

            AssertEqual(testCase.ExpectedResult.Logs.Count, exportBuilder.Logs.Count, "Logs.Count");

            int logCount = 0;
            foreach (var expectedLogEntry in testCase.ExpectedResult.Logs)
            {
                Assert.True(exportBuilder.Logs.Count > logCount);
                var logEntry = exportBuilder.Logs[logCount++];
                AssertEqual(expectedLogEntry.Body, logEntry.Body, "LogEntry.Body");
                AssertEqual(expectedLogEntry.LogLevel, logEntry.LogLevel, "LogEntry.LogLevel");
                AssertEqual(expectedLogEntry.Timestamp, logEntry.Timestamp, "LogLevel.Timestamp");
            }

            this._output.WriteLine($"{DateTime.UtcNow}: Assert logs completed.");

            // Cleanup

            mqttHelper.Dispose();
            await mqttCoordinator.DisconnectAllBrokers();

            this._output.WriteLine($"{DateTime.UtcNow}: Cleanup completed.");

            this._output.WriteLine($"{DateTime.UtcNow}: Test case with id '{testCase.Setup.Id}' completed.");
        }

        /// <summary>
        /// Asserts two values are equal and writes a message to output if not.
        /// </summary>
        /// <typeparam name="T">The type of the values to be compared.</typeparam>
        /// <param name="expected">The expected value.</param>
        /// <param name="actual">The actual value.</param>
        /// <param name="message">The message written in case of an error.</param>
        private void AssertEqual<T>(T expected, T actual, string message)
        {
            try
            {
                Assert.Equal(expected, actual);
            }
            catch
            {
                this._output.WriteLine($"{DateTime.UtcNow}: [ERROR] Assert equal failed: '{message}'");
                throw;
            }
        }
    }
}
