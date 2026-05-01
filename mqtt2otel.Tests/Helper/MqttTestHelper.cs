using MQTTnet;
using MQTTnet.Server;


namespace mqtt2otel.Server.Helper
{
    public static class MqttTestHelper
    {
        private static MqttServer? mqttServer;

        private static IMqttClient? mqttClient;

        public static async Task EnsureServerIsStarted()
        {
            if (mqttServer != null) return;

            var options = new MqttServerOptionsBuilder()
                .WithDefaultEndpoint()
                .WithDefaultEndpointPort(1883)
                .Build();
            mqttServer = new MqttServerFactory().CreateMqttServer(options);
            await mqttServer.StartAsync();
            await Task.Delay(100);

            mqttClient = new MqttClientFactory().CreateMqttClient();

            await mqttClient.ConnectAsync(
                   new MqttClientOptionsBuilder()
                      .WithTcpServer("127.0.0.1", 1883)
                      .Build());

            Assert.True(mqttClient.IsConnected);
        }

        public static async Task PublishPayload(string topic, string payload)
        {
            if (mqttClient == null)
            {
                throw new Exception($"{nameof(PublishPayload)} can only be called, when mqttClient is set. Please call {nameof(EnsureServerIsStarted)} first.");
            }

            var message = new MqttApplicationMessageBuilder()
                                .WithTopic(topic)
                                .WithPayload(payload)
                                .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                                .Build();

            await mqttClient.PublishAsync(message);
        }
    }
}
