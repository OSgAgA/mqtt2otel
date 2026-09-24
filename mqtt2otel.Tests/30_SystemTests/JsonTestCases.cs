using Microsoft.Extensions.Logging;
using Moq;
using mqtt2otel.Helper;
using mqtt2otel.InternalMetrics;
using mqtt2otel.Manifest;
using mqtt2otel.Metadata;
using mqtt2otel.Parser;
using mqtt2otel.Server.Helper;
using mqtt2otel.Shared;
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
        [MemberData(nameof(TestCaseData.LoadAllAsMemberdataTestIds), MemberType = typeof(TestCaseData))]
        public async Task ShouldPassAllJsonTestCases(string testCaseId)
        {
            var mb = GC.GetTotalMemory(forceFullCollection: true) / (1024 * 1024);
            Console.WriteLine($"[mem] before {testCaseId}: {mb} MB");

            TestCaseData testCase = TestCaseData.GetById(testCaseId);

            var culture = new CultureInfo("en-US");
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;

            this._output.WriteLine($"{DateTime.UtcNow}: Executing test case with id: '{testCase.Setup.Id}'");

            // Arrange

            var payloadParser = new PayloadParser();
            var embeddedExpressionParser = new EmbeddedExpressionParser(payloadParser);

            using var mqttHelper = new MqttTestHelper();
            await mqttHelper.EnsureServerIsStarted();

            var dataStores = GenericHelper.GetDataStores(payloadParser, embeddedExpressionParser);
            var manifest = ManifestHelper.ReadManifestFromString(testCase.Setup.Manifest, dataStores, payloadParser, embeddedExpressionParser);
            payloadParser.SetMappings(manifest.Mappings);
            embeddedExpressionParser.SetMappings(manifest.Mappings);

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
            int expectedCount = 1;
            mqttCoordinator.OnMessageProcessed += (sender, args) =>
            {
                if (expectedCount == tcsCount++) tcs.SetResult(args);
            };

            await mqttCoordinator.ConnectAndSubscribe(manifest);

            var internalLogger = new Mock<ILogger<OtelCoordinator>>();
            var exportBuilder = new OtelTestExporterBuilder();
            var otelCoordinator = new OtelCoordinator(internalLogger.Object, exportBuilder, dataStores, new OtelInternalMeter(), embeddedExpressionParser, new ApplicationSettings());
            otelCoordinator.Connect(manifest);

            this._output.WriteLine($"{DateTime.UtcNow}: Arrange completed.");

            // Act

            await mqttHelper.PublishPayload(testCase.Setup.MqttData[0].Topic, testCase.Setup.MqttData[0].Payload, testCase.Setup.MqttData[0].UserProperties);

            var completedTask = await Task.WhenAny(
                               tcs.Task,
                               Task.Delay(1000, TestContext.Current.CancellationToken));

            Assert.True(completedTask == tcs.Task, "Callback was not triggered");

            otelCoordinator.FlushMeters();

            this._output.WriteLine($"{DateTime.UtcNow}: Act completed.");

            // Assert
            // Metrics

            AssertEqual(testCase.ExpectedResults[0].Metrics.Count, exportBuilder.GetAllMetrics().Count(), "metrics.count");

            var metrics = exportBuilder.GetAllMetrics().Select(keyValue => keyValue.Value).ToList();

            int i = 0;
            foreach (var expectedMetric in testCase.ExpectedResults[0].Metrics)
            {
                var metric = metrics[i++];

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

            AssertEqual(testCase.ExpectedResults[0].Logs.Count, exportBuilder.GetAllLogs().Count(), "Logs.Count");

            var logs = exportBuilder.GetAllLogs().Select(keyValue => keyValue.Value).ToList();

            int logCount = 0;
            foreach (var expectedLogEntry in testCase.ExpectedResults[0].Logs)
            {
                Assert.True(exportBuilder.GetAllLogs().Count() > logCount);
                var logEntry = logs[logCount++];
                AssertEqual(expectedLogEntry.Body, logEntry.Body, "LogEntry.Body");
                AssertEqual(expectedLogEntry.LogLevel, logEntry.LogLevel, "LogEntry.LogLevel");
                AssertEqual(expectedLogEntry.Timestamp, logEntry.Timestamp, "LogLevel.Timestamp");
            }

            this._output.WriteLine($"{DateTime.UtcNow}: Assert logs completed.");

            // Cleanup

            await mqttCoordinator.DisconnectAllBrokers();
            mqttHelper.Dispose();

            otelCoordinator.Dispose();

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
