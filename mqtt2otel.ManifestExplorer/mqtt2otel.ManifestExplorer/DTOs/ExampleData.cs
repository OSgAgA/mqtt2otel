using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace mqtt2otel.ManifestExplorer.DTOs
{
    public class ExampleData(string id, string name, string description, string topic, string payload, string manifest)
    {
        private static Dictionary<string, ExampleData> cache = new Dictionary<string, ExampleData>();

        private static void Create()
        {
            if (cache != null && cache.Count > 0) return;

            cache = new Dictionary<string, ExampleData>();

            cache["ex-1"] = new ExampleData
                (
                    id: "ex-1",
                    name: "Simple Dissect log parser",
                    description: "A simple example on how to parse logs using the extended dissect parser.",
                    topic: "message-log-topic",
                    payload: """
                    2026-01-31T15:42Z [INFO] [ServerA] This is a test.
                    """,
                    manifest: """
                    Version: 1.0

                    MqttConnections:
                      - Name: "My broker"

                    OtelConnections:
                      - Name: "My Otel server"
                        ServiceName: "my-service"
                        ServiceNamespace: "my-service-namespace"

                    Processors:
                      - Name: "Server logs"
                        Description: "Collect all log messages from the server."
                        Mqtt:
                          Subscriptions:
                            - Name: "Server logs"
                              Topic: "message-log-topic"
                        Otel:
                          Attributes:
                            - Key: Location
                              Value: MainServerRoom
                          Logs:
                            - Name: "Logging"
                              PayloadType: Json
                              Transform: "DISSECT('%{otel_timestamp} [%{otel_loglevel}] [%{server_name}] %{otel_message}')"
                    """
                );

            cache["ex-2"] = new ExampleData
            (
                id: "ex-2",
                name: "Manual metric from json",
                description: "A simple example on how to parse a json paylog to create detailed metrics manually.",
                topic: "message-topic",
                payload: """
                    {
                       Processor:
                       {
                          Temperature: 42
                       }
                    }
                    """,
                manifest: """
                    Version: 1.0

                    MqttConnections:
                      - Name: "My broker"

                    OtelConnections:
                      - Name: "My Otel server"
                        ServiceName: "my-service"
                        ServiceNamespace: "my-service-namespace"

                    Processors:
                      - Name: "Processor Temperature"
                        Description: "Provides the current processor temperature."
                        Mqtt: 
                          Subscriptions:
                            - Name: "Processor information"
                              Topic: "message-topic"
                        Otel:
                          Metrics:
                            - Name: "Processor.Temperature"
                              Description: "The current processor temperature."
                              SignalDataType: Float
                              Instrument: Gauge
                              Attributes:
                                - Key: "test"
                                  Value: "value"
                              Value: "JSONPATH('$.Processor.Temperature')"
                    """
            );
        }
        public static ExampleData GetExampleById(string id)
        {
            ExampleData.Create();
            if (cache.ContainsKey(id)) return cache[id];

            return cache.First().Value;
        } 

        public static List<ExampleData> GetAll()
        {
            var result = new List<ExampleData>();

            Create();
            foreach (var item in cache)
            {
                result.Add(item.Value);
            }

            return result;
        }

        public string Id { get; set; } = id;

        public string Name { get; set; } = name;

        public string Description { get; set; } = description;

        public string Topic { get; set; } = topic;

        public string Payload { get; set; } = payload;

        public string Manifest { get; set; } = manifest;


    }
}
