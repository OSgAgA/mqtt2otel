using Microsoft.Extensions.Logging;
using Moq;
using mqtt2otel.Helper;
using mqtt2otel.InternalMetrics;
using mqtt2otel.Manifest;
using mqtt2otel.Metadata;
using mqtt2otel.Parser;
using mqtt2otel.Provider;
using mqtt2otel.Server.Helper;
using mqtt2otel.Shared;
using mqtt2otel.Shared.Helper;
using mqtt2otel.Tests.Helper;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Xunit.Sdk;

namespace mqtt2otel.Tests._30_SystemTests
{
    [Collection("MQTT Tests")]
    public class JsonTestCases : IClassFixture<JsonTestsFixture>
    {
        private readonly ITestOutputHelper _output;

        public JsonTestCases(ITestOutputHelper output)
        {
            _output = output;
        }

        [Theory(DisplayName = "{method} ({args})")]
        [MemberData(nameof(TestCaseData.LoadAllAsMemberdataTestIds), MemberType = typeof(TestCaseData))]
        public async Task ShouldPassAllJsonTestCases(string testCaseId)
        {
            // Let's keep this in, to track memory consumption. Can be removed if memory is tracked via otel.
            var mb = GC.GetTotalMemory(forceFullCollection: false) / (1024 * 1024);
            Console.WriteLine($"[mem] before {testCaseId}: {mb} MB");

            TestCaseData testCase = TestCaseData.GetById(testCaseId);

            var culture = new CultureInfo("en-US");
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;

            this._output.WriteLine($"{DateTime.UtcNow}: Executing test case with id: '{testCase.Setup.Id}'");


            // Arrange

            if (JsonTestsFixture.mqttHelper != null) await JsonTestsFixture.mqttHelper.ConnectClient();

            var payloadParser = new PayloadParser();
            var embeddedExpressionParser = new EmbeddedExpressionParser(payloadParser);

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

            manifest.WaitForLateSpanEventsInS = 0;
            manifest.Initialize();

            // Skip all further test, if only manifest should be validated.
            if (testCase.Setup.ValidateManifestOnly) return;

            var loggerMockMqtt = new Mock<ILogger<MqttCoordinator>>();
            var traceStore = new LocalTraceStore(new FakeTraceIdProvider(null));
            var messageProcessor = MessageProcessorAccessor.CreateNew(payloadParser, embeddedExpressionParser, dataStores, traceStore);
            var mqttCoordinator = new MqttCoordinator(loggerMockMqtt.Object, new MqttMeter(), messageProcessor);

            this._output.WriteLine($"{DateTime.UtcNow}: Arrange completed.");

            try
            {
                int currentIndex = 0;

                foreach (var mqttData in testCase.Setup.MqttData)
                {
                    var internalLogger = new Mock<ILogger<OtelCoordinator>>();
                    var exportBuilder = new OtelTestExporterBuilder();

                    await mqttCoordinator.ConnectAndSubscribe(manifest);

                    using (var otelCoordinator = new OtelCoordinator(internalLogger.Object, exportBuilder, dataStores, new OtelInternalMeter(), embeddedExpressionParser, new ApplicationSettings(), new OtelCollector(isActive: false)))
                    {
                        otelCoordinator.Connect(manifest);
                        this._output.WriteLine($"{DateTime.UtcNow}: Processing message {currentIndex + 1}/{testCase.Setup.MqttData.Count}.");

                        await this.SendMessage(mqttCoordinator, mqttData);
                        var completedSpans = traceStore.GetCompletedOrTimeOutSpans();
                        otelCoordinator.CreateSpans(completedSpans);
                        
                    } // explicit dispose triggers flush.

                    this.AssertMetricsExpectations(testCase.ExpectedResults[currentIndex].Metrics, exportBuilder);
                    this.AssertLogExpectations(testCase.ExpectedResults[currentIndex], exportBuilder);
                    this.AssertTraceExpectations(testCase.ExpectedResults[currentIndex].Spans, exportBuilder);
                    this.AssertInternalStateExpectations(testCase.ExpectedResults[currentIndex].State, traceStore.GetFullState());

                    currentIndex++;
                }

                this._output.WriteLine($"{DateTime.UtcNow}: Act completed.");
            }
            finally
            {
                // Cleanup

                await mqttCoordinator.DisconnectAllBrokers();
                if (JsonTestsFixture.mqttHelper != null) await JsonTestsFixture.mqttHelper.DisconnectClient();

                this._output.WriteLine($"{DateTime.UtcNow}: Cleanup completed.");

                this._output.WriteLine($"{DateTime.UtcNow}: Test case with id '{testCase.Setup.Id}' completed.");
            }
        }

        /// <summary>
        /// Asserts that all expectations of logs are meat.
        /// </summary>
        /// <param name="result">The expected test result.</param>
        /// <param name="exportBuilder"></param>
        private void AssertLogExpectations(ExpectedTestResult result, OtelTestExporterBuilder exportBuilder)
        {
            this._output.WriteLine($"{DateTime.UtcNow}: Asserting expected log results.");

            AssertEqual(result.Logs.Count, exportBuilder.GetAllLogs().Count(), "Logs.Count");

            var logs = exportBuilder.GetAllLogs().Select(keyValue => keyValue.Value).ToList();

            int logCount = 0;
            foreach (var expectedLogEntry in result.Logs)
            {
                Assert.True(exportBuilder.GetAllLogs().Count() > logCount);
                var logEntry = logs[logCount++];
                AssertEqual(expectedLogEntry.Body, logEntry.Body, "LogEntry.Body");
                AssertEqual(expectedLogEntry.LogLevel, logEntry.LogLevel, "LogEntry.LogLevel");

                var testData = new LogTestData(logEntry, new Shated.OtelServerConnectionInfo());
                AssertEqual(expectedLogEntry.TraceId, testData.TraceId, "LogEntry.TraceId");
                AssertEqual(expectedLogEntry.SpanId, testData.SpanId, "LogEntry.SpanId");
            }

            this._output.WriteLine($"{DateTime.UtcNow}: Assert logs completed.");
        }

        /// <summary>
        /// Asserts that all expectations for metrics are meat.
        /// </summary>
        /// <param name="metricExpectations">The expectations to test against.</param>
        /// <param name="exportBuilder">The export builder used when the message was processed.</param>
        private void AssertMetricsExpectations(List<MetricTestData> metricExpectations, OtelTestExporterBuilder exportBuilder)
        {
            this._output.WriteLine($"{DateTime.UtcNow}: Asserting expected metrics results.");

            AssertEqual(metricExpectations.Count, exportBuilder.GetAllMetrics().Count(), "metrics.count");

            var metrics = exportBuilder.GetAllMetrics().Select(keyValue => keyValue.Value).ToList();

            int receivedMetricIndex = 0;

            foreach (var expectedMetric in metricExpectations)
            {
                var metric = metrics[receivedMetricIndex++];

                AssertEqual(expectedMetric.Name, metric.Name, "Metric.Name");
                AssertEqual(expectedMetric.MetricType, metric.MetricType, "Metric.Type");
                AssertEqual(expectedMetric.Unit, metric.Unit, "Metric.Unit");
                AssertEqual(expectedMetric.Description, metric.Description, "Metric.Description");

                int count = 0;
                foreach (var metricPoint in metric.GetMetricPoints())
                {
                    Assert.True(expectedMetric.MetricPoints.Count > count);
                    var expectedPoint = expectedMetric.MetricPoints[count];
                    count++;
                    AssertEqual(expectedPoint.Value.ToString(), metricPoint.GetValueAsObject(metric.MetricType).ToString(), "MetricPoint.Value");
                    AssertEqual(expectedPoint.Tags.Count, metricPoint.Tags.Count, "tags.count");

                    foreach (var tag in metricPoint.Tags)
                    {
                        Assert.True(expectedPoint.Tags.ContainsKey(tag.Key));
                        AssertEqual(expectedPoint.Tags[tag.Key]?.ToString(), tag.Value?.ToString(), $"Tag.Value for key {tag.Key}");
                    }
                }

                AssertEqual(expectedMetric.MetricPoints.Count, count, "metricPoints.Count");
            }

            this._output.WriteLine($"{DateTime.UtcNow}: Assert metrics completed.");
        }

        /// <summary>
        /// Asserts that all expectations for metrics are meat.
        /// </summary>
        /// <param name="spanExpectations">The expectations to test against.</param>
        /// <param name="exportBuilder">The export builder used when the message was processed.</param>
        private void AssertTraceExpectations(List<SpanTestData> spanExpectations, OtelTestExporterBuilder exportBuilder)
        {
            this._output.WriteLine($"{DateTime.UtcNow}: Asserting expected trace results.");

            AssertEqual(spanExpectations.Count, exportBuilder.GetAllSpans().Count(), "traces.count");

            var traces = exportBuilder.GetAllSpans().Select(keyValue => keyValue.Value).ToList();

            int receivedMetricIndex = 0;

            foreach (var expectedSpan in spanExpectations)
            {
                var span = traces[receivedMetricIndex++];

                AssertEqual(expectedSpan.Name, span.DisplayName, "Span.Name");
                AssertEqual(expectedSpan.Kind, span.Kind.ToString(), "Span.Kind");
                AssertEqual(expectedSpan.Status, span.Status.ToString(), "Span.Status");
                AssertEqual(expectedSpan.SpanId, span.Tags.Where(kv => kv.Key == InternalAttributes.SpanId).First().Value?.ToString(), "Span.SpanId");
                AssertEqual(expectedSpan.TraceId, span.Tags.Where(kv => kv.Key == InternalAttributes.TraceId).First().Value?.ToString(), "Span.TraceId");

                AssertTags(span.TagObjects.Select( tag => new KeyValuePair<string, string?>(tag.Key, tag.Value?.ToString())), expectedSpan.Attributes.ToAttributeStringDictionary(), $"Span '{span.DisplayName}' tags");

                int evtIndex = 0;
                foreach (var evt in span.Events)
                {
                    var expectedEvent = expectedSpan.Events[evtIndex++];

                    AssertEqual(expectedEvent.Name, evt.Name, "Event Name");
                    AssertTags(evt.Tags, expectedEvent.Attributes.ToDictionary(), $"Event {evt.Name} tags");
                }

                int linkIndex = 0;
                foreach (var link in span.Links)
                {
                    var expectedLink = expectedSpan.Links[linkIndex++];

                    AssertTags(link.Tags ?? new List<KeyValuePair<string, object?>>(), expectedLink.Tags?.ToDictionary() ?? new Dictionary<string, object?>(), $"Event({linkIndex}) link tags");
                }
            }

            this._output.WriteLine($"{DateTime.UtcNow}: Assert traces completed.");
        }

        /// <summary>
        /// Asserts that all expectations of the internal state are meat.
        /// </summary>
        /// <param name="stateExpectations">The expected state.</param>
        /// <param name="state">The actual state.</param>
        private void AssertInternalStateExpectations(InternalState stateExpectations, InternalState state)
        {
            this._output.WriteLine($"{DateTime.UtcNow}: Asserting expected state results.");

            AssertEqual(stateExpectations.Spans.Count, state.Spans.Count(), "spans.count");

            int receivedSpanIndex = 0;

            foreach (var expectedSpan in stateExpectations.Spans)
            {
                var span = state.Spans[receivedSpanIndex++];

                AssertEqual(expectedSpan.Name, span.Name, "Span.Name");
                AssertEqual(expectedSpan.Kind, span.Kind, "Span.Kind");
                AssertEqual(expectedSpan.Status, span.Status, "Span.Status");

                var actualAttributes = span.Attributes.Select(tag => new KeyValuePair<string, string?>(tag.Key, tag.Value?.ToString()));
                var expectedAttributes = expectedSpan.Attributes.ToAttributeStringDictionary();
                AssertTags(actualAttributes, expectedAttributes, $"Span '{span.Name}' tags");

                int evtIndex = 0;
                foreach (var evt in span.Events)
                {
                    var expectedEvent = expectedSpan.Events[evtIndex++];

                    AssertEqual(expectedEvent.Name, evt.Name, "Event Name");
                    AssertTags(evt.Attributes.Select(tag => new KeyValuePair<string, string?>(tag.Key, tag.Value?.ToString())), expectedEvent.Attributes.ToAttributeStringDictionary(), $"Event {evt.Name} tags");
                }

                int linkIndex = 0;
                foreach (var link in span.LinkTo)
                {
                    var expectedLink = expectedSpan.LinkTo[linkIndex++];

                    AssertTags(link.Attributes.Select(tag => new KeyValuePair<string, string?>(tag.Key, tag.Value?.ToString())), expectedLink.Attributes.ToAttributeStringDictionary(), $"Event({linkIndex}) link tags");
                }
            }

            int contextIndex = 0;
            foreach (var expectedContext in stateExpectations.SpanContexts)
            {
                var context = state.SpanContexts[contextIndex++];

                AssertEqual(expectedContext.Id, context.Id, $"context.Id");
            }
        }

        /// <summary>
        /// Asserts that two tag lists are equal.
        /// </summary>
        /// <param name="tags">The tags to test.</param>
        /// <param name="expected">The expected result.</param>
        /// <param name="id">An id to identify the use case in case of an error.</param>
        private void AssertTags(IEnumerable<KeyValuePair<string, string?>> tags, Dictionary<string, string?> expected, string id)
        {
            var convertedTags = tags.Where(tag => !OtelAttribute.TestForInternalAttributeName(tag.Key)).Select(tag => new KeyValuePair<string, object?>(tag.Key, tag.Value));
            var convertedExpectedTags = expected.Where(tag => !OtelAttribute.TestForInternalAttributeName(tag.Key)).Select(tag => new KeyValuePair<string, object?>(tag.Key, tag.Value));

            this.AssertTags(convertedTags, convertedExpectedTags.ToDictionary(), id);
        }

        /// <summary>
        /// Asserts that two tag lists are equal.
        /// </summary>
        /// <param name="tags">The tags to test.</param>
        /// <param name="expected">The expected result.</param>
        /// <param name="id">An id to identify the use case in case of an error.</param>
        private void AssertTags(IEnumerable<KeyValuePair<string, object?>> tags, Dictionary<string, object?> expected, string id)
        {
            var filteredTags = tags.Where(tag => !OtelAttribute.TestForInternalAttributeName(tag.Key));
            AssertEqual(expected.Where( attribute => !OtelAttribute.TestForInternalAttributeName(attribute.Key)).Count(), filteredTags.Count(), $"{id} count");

            foreach (var tag in filteredTags)
            {
                if (!OtelAttribute.TestForInternalAttributeName(tag.Key))
                {
                    Assert.True(expected.ContainsKey(tag.Key));
                    AssertEqual(expected[tag.Key]?.ToString(), tag.Value?.ToString(), $"{id} - Tag.Value for key {tag.Key}");
                }
            }
        }

        /// <summary>
        /// Sends a message to the provided mqtt coordinator.
        /// </summary>
        /// <param name="mqttCoordinator">The mqtt coordinator, that the message will be send to.</param>
        /// <param name="mqttData">The message data.</param>
        private async Task SendMessage(MqttCoordinator mqttCoordinator, MqttSetupData mqttData)
        {
            var tcs = new TaskCompletionSource<MqttMessageReceivedEventArgs>();

            EventHandler<MqttMessageReceivedEventArgs> handler = (sender, args) => tcs.TrySetResult(args);
            mqttCoordinator.OnMessageProcessed += handler;

            try
            {
                await JsonTestsFixture.mqttHelper!.PublishPayload(mqttData);

                var completedTask = await Task.WhenAny(
                                   tcs.Task,
                                   Task.Delay(1000, TestContext.Current.CancellationToken));

                Assert.True(completedTask == tcs.Task, "Callback was not triggered");
            }
            finally
            {
                mqttCoordinator.OnMessageProcessed -= handler;
            }
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
