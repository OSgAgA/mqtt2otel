using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using Moq;
using mqtt2otel.InternalMetrics;
using mqtt2otel.Parser;
using mqtt2otel.Server.Helper;
using mqtt2otel.Stores;
using mqtt2otel.Tests.Helper;
using mqtt2otel.Transformation;
using MQTTnet;
using MQTTnet.Internal;
using MQTTnet.Server;
using System;
using System.Collections.Generic;
using System.Text;

namespace mqtt2otel.Tests._20_IntegrationTests

{
    [Collection("MQTT Tests")]
    public class MqttIntegrationTests
    {
        [Fact]
        public async Task ShouldReceiveMessageFromMqttServer()
        {
            using var mqttHelper = new MqttTestHelper();
            await mqttHelper.EnsureServerIsStarted();

            var yaml = """
                       Version: 1.0
                
                       MqttConnections:
                         - ClientPrefix: "mqtt2otel-dev"
                           Endpoint:
                             Port: 1883
                             Address: "127.0.0.1"
                             ConnectionType: Tcp
                             EnableTls: false

                       Processors:
                       - Name: "Test processor"
                         Mqtt:
                           Subscriptions:
                            - Name: "Temperature"
                              Topic: "sensors/temperature"
                       """;

            var manifest = ManifestHelper.ReadManifestFromString(yaml);

            var loggerMockMqtt = new Mock<ILogger<MqttCoordinator>>();
            var mqttCoordinator = new MqttCoordinator(loggerMockMqtt.Object, new MqttMeter());
            var tcs = new TaskCompletionSource<MqttMessageReceivedEventArgs>();
            mqttCoordinator.OnMessageReceived += (sender, args) => tcs.SetResult(args);

            await mqttCoordinator.ConnectAndSubscribe(manifest);

            string topic = "sensors/temperature";
            string payload = "42";
            
            await mqttHelper.PublishPayload(topic, payload);

            var completedTask = await Task.WhenAny(
                               tcs.Task,
                               Task.Delay(1000, TestContext.Current.CancellationToken));

            Assert.True(completedTask == tcs.Task, "Callback was not triggered");

            var result = await tcs.Task;

            Assert.Equal(topic, result?.Message.Topic);
            Assert.Equal(payload, result?.Message.Payload);
            Assert.Equal("Temperature", result?.Subscription?.Name);
            Assert.Equal("Test processor", result?.Processor?.Name);
        }

        [Fact]
        public async Task ShouldReceiveMessageWithCorrectSubscription()
        {
            using var mqttHelper = new MqttTestHelper();
            await mqttHelper.EnsureServerIsStarted();

            var yaml = """
                       Version: 1.0
                
                       MqttConnections:
                         - ClientPrefix: "mqtt2otel-dev"
                           Endpoint:
                             Port: 1883
                             Address: "127.0.0.1"
                             ConnectionType: Tcp
                             EnableTls: false

                       Processors:
                       - Name: "Test processor"
                         Mqtt:
                           Subscriptions:
                            - Name: "Temperature"
                              Topic: "sensors/temperature"
                            - Name: "Do not use"
                              Topic: "donotuse"
                       """;

            var manifest = ManifestHelper.ReadManifestFromString(yaml);

            var loggerMockMqtt = new Mock<ILogger<MqttCoordinator>>();
            var mqttCoordinator = new MqttCoordinator(loggerMockMqtt.Object, new MqttMeter());
            var tcs = new TaskCompletionSource<MqttMessageReceivedEventArgs>();
            mqttCoordinator.OnMessageReceived += (sender, args) => tcs.SetResult(args);

            await mqttCoordinator.ConnectAndSubscribe(manifest);

            string topic = "sensors/temperature";
            string payload = "42";

            await mqttHelper.PublishPayload(topic, payload);

            var completedTask = await Task.WhenAny(
                               tcs.Task,
                               Task.Delay(1000, TestContext.Current.CancellationToken));

            Assert.True(completedTask == tcs.Task, "Callback was not triggered");

            var result = await tcs.Task;

            Assert.Equal(topic, result.Message.Topic);
            Assert.Equal(payload, result.Message.Payload);
            Assert.Equal("Temperature", result?.Subscription?.Name);
            Assert.Equal("Test processor", result?.Processor?.Name);
        }

        [Fact]
        public async Task ShouldProcessMessageWithCorrectProcessor()
        {
            using var mqttHelper = new MqttTestHelper();
            await mqttHelper.EnsureServerIsStarted();

            var yaml = """
                       Version: 1.0
                
                       MqttConnections:
                         - ClientPrefix: "mqtt2otel-dev"
                           Endpoint:
                             Port: 1883
                             Address: "127.0.0.1"
                             ConnectionType: Tcp
                             EnableTls: false

                       Processors:
                       - Name: "Test processor"
                         Mqtt:
                           Subscriptions:
                            - Name: "Temperature"
                              Topic: "sensors/temperature"
                       - Name: "Do not use processor"
                         Mqtt:
                           Subscriptions:
                            - Name: "Do not use"
                              Topic: "donotuse"
                       """;

            var manifest = ManifestHelper.ReadManifestFromString(yaml);

            var loggerMockMqtt = new Mock<ILogger<MqttCoordinator>>();
            var mqttCoordinator = new MqttCoordinator(loggerMockMqtt.Object, new MqttMeter());
            var tcs = new TaskCompletionSource<MqttMessageReceivedEventArgs>();

            mqttCoordinator.OnMessageReceived += (sender, args) => tcs.SetResult(args);

            await mqttCoordinator.ConnectAndSubscribe(manifest);

            string topic = "sensors/temperature";
            string payload = "42";

            await mqttHelper.PublishPayload(topic, payload);

            var completedTask = await Task.WhenAny(
                               tcs.Task,
                               Task.Delay(1000, TestContext.Current.CancellationToken));

            Assert.True(completedTask == tcs.Task, "Callback was not triggered");

            var result = await tcs.Task;

            Assert.Equal(topic, result.Message.Topic);
            Assert.Equal(payload, result.Message.Payload);
            Assert.Equal("Temperature", result?.Subscription?.Name);
            Assert.Equal("Test processor", result?.Processor?.Name);
        }

        [Fact]
        public async Task ShouldProcessMessageWithAllProcessors()
        {
            using var mqttHelper = new MqttTestHelper();
            await mqttHelper.EnsureServerIsStarted();

            var yaml = """
                       Version: 1.0
                
                       MqttConnections:
                         - ClientPrefix: "mqtt2otel-dev"
                           Endpoint:
                             Port: 1883
                             Address: "127.0.0.1"
                             ConnectionType: Tcp
                             EnableTls: false

                       Processors:
                       - Name: "First processor"
                         Mqtt:
                           Subscriptions:
                            - Name: "Temperature"
                              Topic: "sensors/temperature"
                       - Name: "Second processor"
                         Mqtt:
                           Subscriptions:
                            - Name: "Temperature"
                              Topic: "sensors/temperature"                       
                       """;

            var manifest = ManifestHelper.ReadManifestFromString(yaml);

            var loggerMockMqtt = new Mock<ILogger<MqttCoordinator>>();
            var mqttCoordinator = new MqttCoordinator(loggerMockMqtt.Object, new MqttMeter());
            var tcs = new TaskCompletionSource<List<MqttMessageReceivedEventArgs>>();
            List<MqttMessageReceivedEventArgs> results = new();

            mqttCoordinator.OnMessageReceived += (sender, args) =>
            {
                results.Add(args);
                if (results.Count == 2)
                {
                    tcs.SetResult(results);
                }
            };

            await mqttCoordinator.ConnectAndSubscribe(manifest);

            string topic = "sensors/temperature";
            string payload = "42";

            await mqttHelper.PublishPayload(topic, payload);

            var completedTask = await Task.WhenAny(
                               tcs.Task,
                               Task.Delay(1000, TestContext.Current.CancellationToken));

            Assert.True(completedTask == tcs.Task, "Callback was not triggered");

            Assert.Equal(2, results.Count);

            Assert.Equal(topic, results[0].Message.Topic);
            Assert.Equal(payload, results[0].Message.Payload);
            Assert.Equal("Temperature", results[0]?.Subscription?.Name);
            Assert.Equal("First processor", results[0]?.Processor?.Name);

            Assert.Equal(topic, results[1].Message.Topic);
            Assert.Equal(payload, results[1].Message.Payload);
            Assert.Equal("Temperature", results[1]?.Subscription?.Name);
            Assert.Equal("Second processor", results[1]?.Processor?.Name);

        }
    }
}
