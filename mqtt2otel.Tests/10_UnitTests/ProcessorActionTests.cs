using Microsoft.Extensions.Logging;
using Moq;
using mqtt2otel.Helper;
using mqtt2otel.Parser;
using mqtt2otel.Stores;
using mqtt2otel.Tests.Helper;
using mqtt2otel.Transformation;
using System;
using System.Collections.Generic;
using System.Text;

namespace mqtt2otel.Tests._10_UnitTests
{
    public class ProcessorActionTests
    {
        private const string ManifestTemplate = """
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
                            - Name: "Sensor"
                              Topic: "sensors/device"
                         Otel:
                           Metrics:
                             - Instrument: Gauge
                               ParseAs:
                                 Type: Json
                                 NameOnly: true
                               Actions:
                       """;

        /// <summary>
        /// Processes a payload with the given actions and returns the names of all signals that reached the signal store.
        /// </summary>
        private static List<string> ProcessAndGetStoredSignals(string actionsYaml, string payload)
        {
            var yaml = ManifestTemplate + Environment.NewLine + actionsYaml;

            var payloadParser = new PayloadParser();
            var embeddedExpressionParser = new EmbeddedExpressionParser(payloadParser);

            var signalStore = new SignalStore(embeddedExpressionParser);
            var storedSignals = new List<string>();

            // The signal creator is only called for signals that are not ignored and are written to the store.
            // It must create the metric, otherwise the first stored signal would abort processing of the remaining ones.
            signalStore.SignalCreator = (measurement, context) =>
            {
                storedSignals.Add(measurement.SignalName);

                switch (measurement.Value)
                {
                    case double doubleValue:
                        signalStore.StoreValue(measurement, new OtelMetric<double>(doubleValue, string.Empty, string.Empty, measurement.Attributes));
                        break;
                    case long longValue:
                        signalStore.StoreValue(measurement, new OtelMetric<long>(longValue, string.Empty, string.Empty, measurement.Attributes));
                        break;
                    default:
                        throw new NotSupportedException($"Unexpected value type {measurement.Value?.GetType()} in test.");
                }
            };

            var loggerStore = new LoggerStore(new Mock<ILogger<string>>().Object, payloadParser, new PayloadTransformation(), embeddedExpressionParser);
            var dataStores = new DataStores(signalStore, loggerStore);

            var manifest = ManifestHelper.ReadManifestFromString(yaml, dataStores, payloadParser, embeddedExpressionParser);
            payloadParser.SetMappings(manifest.Mappings);
            embeddedExpressionParser.SetMappings(manifest.Mappings);
            manifest.Initialize();

            var processor = manifest.Processors[0];
            var subscription = processor.Mqtt.Subscriptions[0];

            processor.ProcessSubscriptionPayload(new MqttMessage(topic: "sensors/device", payload: payload), subscription);

            return storedSignals;
        }

        [Fact]
        public void ShouldNotStoreAnySignalWhenAllSignalsAreIgnored()
        {
            // Booleans and DateTimes are not valid signal data types. If Ignore is not honored, they would be processed and fail.
            var payload = """{"temperature": 21.5, "running": false, "last_seen": "2026-10-09T16:51:48+02:00"}""";

            var actions = """
                                   - Name: "Ignore everything"
                                     When:
                                       - "1 == 1"
                                     Then:
                                       Ignore: true
                          """;

            var storedSignals = ProcessAndGetStoredSignals(actions, payload);

            Assert.Empty(storedSignals);
        }

        [Fact]
        public void ShouldOnlyIgnoreSignalsMatchingTheCondition()
        {
            var payload = """{"temperature": 21.5, "humidity": 45.0, "linkquality": 32}""";

            var actions = """
                                   - Name: "Only keep temperature"
                                     When:
                                       - "[Name] != 'temperature'"
                                     Then:
                                       Ignore: true
                          """;

            var storedSignals = ProcessAndGetStoredSignals(actions, payload);

            Assert.Equal(new[] { "temperature" }, storedSignals);
        }
    }
}
