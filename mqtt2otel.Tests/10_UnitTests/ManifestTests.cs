using mqtt2otel.Tests.Helper;
using System;
using System.Collections.Generic;
using System.Text;

namespace mqtt2otel.Tests._10_UnitTests
{
    public class ManifestTests
    {
        [Fact]
        public void ShouldParseManifestWithAllPropertiesSet()
        {
            var yaml = """
                       Version: 1.0
                
                       MqttConnections:
                         - Name: "TestName"
                           Description: "TestDescription"
                           ClientPrefix: "mqtt2otel-dev"
                           ReconnectDelayInMs: 2000
                           Endpoint:
                             Port: 1883
                             Address: "127.0.0.1"
                             ConnectionType: Tcp
                             EnableTls: false
                             Protocol: "https://"
                             MqttProtocollVersion: V310
                             TlsSslProtocol: Tls11
                             TlsCaFilePath: "c:\\"
                             UsePacketFragmentation: false
                             Username: "It's me"
                             Password: "Top secret"

                       OtelConnections:
                         - Name: "My Otel server"
                           Description: "My description"
                           ServiceName: "my-service"
                           ServiceVersion: "1.0"
                           ServiceNamespace: "my-service-namespace"
                           Endpoint:
                             Protocol: "http"
                             Port: 4317
                             Address: "my-otel-collector.net"
                             EnableTls: false
                             Headers: "test"                         
                             BatchTimeoutInMs: 1000
                             ClientCertificatePath: "C:\\"
                             ClientCertificatePassword: "Top secret"
                           OtlpExportProtocol: Grpc
                           ExportProcessorType: Simple
                           ClientPrefix: "my-client"

                       SubscriptionGroups:
                         - Name: "Power sensors"
                           Description: "My description"
                           Variables:
                             - Key: "MyKey"
                               Value: "value"
                           BrokerConnection: "TestName"
                           Transform: "NoTransform"
                           Subscriptions:
                             - Name: "Power Sensor 1"
                               Description: "My description"
                               Topic: "1234"
                               BrokerConnection: "TestName"
                               Transform: "No transform"
                               Variables:
                                 - Key: "SensorName"
                                   Value: "WashingMachine"
                         
                       Processors:
                         - Name: "Test processor"
                           Description: "My description"
                           OtelConnection: "My Otel server"
                           Mqtt:
                             Name: "My section"
                             Description: "My description"
                             Variables:
                               - Key: "MyKey"
                                 Value: "MyValue"
                             Subscriptions:
                               - Name: "Temperature"
                                 Topic: "sensors/temperature"
                             SubscriptionGroups:
                               - Name: "Power sensors"
                                 ParentPath: "sensor/"
                                 SubPath: "blahblah"
                             BrokerConnection: "TestName"
                             Transform: "NoTransform"
                         
                           Otel:
                             Name: "My otel section"
                             Description: "My description"
                             OtelConnection: "My Otel server"
                             Attributes:
                               - Key: SensorName
                                 Value: $SensorName
                             Metrics:
                               - Name: "My metric"
                                 Description: "My description"
                                 Attributes:
                                   - Key: "My attribute"
                                     Value: "My value"
                                 OtelConnection: "My Otel server"
                                 Instrument: Histogram
                                 SignalDataType: Double
                                 Unit: "C"
                                 Value: "JSONPATH('$.Temperature')"
                                 HistogramBucketBoundaries:
                                   - 0
                                   - 10
                                   - 20
                                   - 30
                                   - 50
                                   - 70
                                   - 100
                         
                             Logs:
                               - Name: "My log entry"
                                 Description: "My description"
                                 Attributes:
                                   - Key: "My attribute"
                                     Value: "My value"
                                 OtelConnection: "My Otel server"
                                 Filter: "REGEX('.*')"
                                 PayloadType: "Text"
                                 CategoryName: "mqtt2otel"
                                 Transform: "GROK('')"
                                 MessageKey: "my_message_key"
                                 LogLevelKey: "my_log_level_key"

                       """;

            var manifest = ManifestHelper.ReadManifestFromString(yaml);
        }

        [Fact]
        public void ShouldPropagateSubscriptionGroupProperties()
        {
            var yaml = """
                       Version: 1.0
                
                       MqttConnections:
                         - Name: "Default broker"
                           Endpoint:
                             Port: 1883
                             Address: "127.0.0.1"
                       
                         - Name: "Second broker"
                           Endpoint:
                             Port: 1883
                             Address: "127.0.0.1"
                       

                       OtelConnections:
                         - Name: "Default Otel server"
                           Endpoint:
                             Port: 1883
                             Address: "127.0.0.1"
                       
                         - Name: "Second Otel server"
                           Endpoint:
                             Port: 1883
                             Address: "127.0.0.1"

                       SubscriptionGroups:
                         - Name: "Power sensors"
                           BrokerConnection: "Second broker"
                           Transform: "Test"
                           Variables:
                             - Key: "TestKey"
                               Value: "TestValue"
                           Subscriptions:
                             - Name: "Sensor 1"
                               Topic: "1234"
                               BrokerConnection: "Default broker"
                             - Name: "Sensor 2"
                               Topic: "5432"
                               Transform: "New Transform"                               
                       """;

            var manifest = ManifestHelper.ReadManifestFromString(yaml);
            manifest.Initialize();

            Assert.Equal("Second broker", manifest.SubscriptionGroups[0].BrokerConnection);
            Assert.Equal("Default broker", manifest.SubscriptionGroups[0].Subscriptions[0].BrokerConnection);
            Assert.Equal("Second broker", manifest.SubscriptionGroups[0].Subscriptions[1].BrokerConnection);
            Assert.Equal("Test", manifest.SubscriptionGroups[0].Subscriptions[0].Transform);
            Assert.Equal("New Transform", manifest.SubscriptionGroups[0].Subscriptions[1].Transform);

            foreach (var subscription in manifest.SubscriptionGroups[0].Subscriptions)
            {
                Assert.Single(subscription.Variables);
                Assert.Equal("TestKey", subscription.Variables[0].Key);
                Assert.Equal("TestValue", subscription.Variables[0].Value);
            }
        }

        [Fact]
        public void ShouldPropagateProcessorProperties()
        {
            var yaml = """
                       Version: 1.0
                
                       MqttConnections:
                         - Name: "Default broker"
                           Endpoint:
                             Port: 1883
                             Address: "127.0.0.1"
                       
                         - Name: "Second broker"
                           Endpoint:
                             Port: 1883
                             Address: "127.0.0.1"
                       

                       OtelConnections:
                         - Name: "Default Otel server"
                           Endpoint:
                             Port: 1883
                             Address: "127.0.0.1"
                       
                         - Name: "Second Otel server"
                           Endpoint:
                             Port: 1883
                             Address: "127.0.0.1"

                       Processors:
                         - Name: "Test processor"
                           OtelConnection: "Second Otel server"
                           Mqtt:
                             BrokerConnection: "Second broker"
                             Variables:
                               - Key: "MyKey"
                                 Value: "MyValue"
                             Subscriptions:
                               - Name: "sub 1"
                                 Topic: "sensors/temperature"
                                 Transform: "PleaseTransform"
                               - Name: "sub 2"
                                 Topic: "sensors/temperature2"
                                 BrokerConnection: "Default broker"
                             Transform: "NoTransform"
                         
                           Otel:
                             Name: "My otel section"
                             Metrics:
                               - Name: "My metric"
                                 OtelConnection: "Default Otel server"
                               - Name: "My metric"
                         
                             Logs:
                               - Name: "My log entry"
                                 OtelConnection: "Default Otel server"
                               - Name: "My log entry"
                                                                                           
                       """;

            var manifest = ManifestHelper.ReadManifestFromString(yaml);
            manifest.Initialize();

            Assert.Equal("Second Otel server", manifest.Processors[0].OtelConnection);
            Assert.Equal("Second Otel server", manifest.Processors[0].OtelConnection);
            Assert.Equal("Default Otel server", manifest.Processors[0].Otel.Metrics[0].OtelConnection);
            Assert.Equal("Second Otel server", manifest.Processors[0].Otel.Metrics[1].OtelConnection);
            Assert.Equal("Default Otel server", manifest.Processors[0].Otel.Logs[0].OtelConnection);
            Assert.Equal("Second Otel server", manifest.Processors[0].Otel.Logs[1].OtelConnection);

            Assert.Equal("Second broker", manifest.Processors[0].Mqtt.BrokerConnection);
            Assert.Equal("Second broker", manifest.Processors[0].Mqtt.Subscriptions[0].BrokerConnection);
            Assert.Equal("Default broker", manifest.Processors[0].Mqtt.Subscriptions[1].BrokerConnection);

            Assert.Equal("NoTransform", manifest.Processors[0].Mqtt.Transform);
            Assert.Equal("PleaseTransform", manifest.Processors[0].Mqtt.Subscriptions[0].Transform);
            Assert.Equal("NoTransform", manifest.Processors[0].Mqtt.Subscriptions[1].Transform);

            foreach (var subscription in manifest.Processors[0].Mqtt.Subscriptions)
            {
                Assert.Single(subscription.Variables);
                Assert.Equal("MyKey", subscription.Variables[0].Key);
                Assert.Equal("MyValue", subscription.Variables[0].Value);
            }
        }

        [Fact]
        public void ShouldPropagateSubscriptionGroupsToSubscriptions()
        {
            var yaml = """
                       Version: 1.0
                
                       MqttConnections:
                         - Name: "Default broker"
                           Endpoint:
                             Port: 1883
                             Address: "127.0.0.1"
                       
                         - Name: "Second broker"
                           Endpoint:
                             Port: 1883
                             Address: "127.0.0.1"
                       

                       OtelConnections:
                         - Name: "Default Otel server"
                           Endpoint:
                             Port: 1883
                             Address: "127.0.0.1"
                       
                         - Name: "Second Otel server"
                           Endpoint:
                             Port: 1883
                             Address: "127.0.0.1"

                       SubscriptionGroups:
                         - Name: "Power sensors"
                           BrokerConnection: "Second broker"
                           Transform: "Test"
                           Variables:
                             - Key: "TestKey"
                               Value: "TestValue"
                           Subscriptions:
                             - Name: "Sensor 1"
                               Topic: "1234"
                             - Name: "Sensor 2"
                               Topic: "5432"
                       
                       Processors:
                         - Name: "Test processor"
                           Mqtt:
                             SubscriptionGroups:
                               - Name: "Power sensors"
                       
                       """;

            var manifest = ManifestHelper.ReadManifestFromString(yaml);
            manifest.Initialize();

            Assert.Equal(2, manifest.Processors[0].Mqtt.Subscriptions.Count);
            Assert.Equal("Sensor 1", manifest.Processors[0].Mqtt.Subscriptions[0].Name);
            Assert.Equal("1234", manifest.Processors[0].Mqtt.Subscriptions[0].Topic);
            Assert.Equal("Sensor 2", manifest.Processors[0].Mqtt.Subscriptions[1].Name);
            Assert.Equal("5432", manifest.Processors[0].Mqtt.Subscriptions[1].Topic);
        }

    }
}
