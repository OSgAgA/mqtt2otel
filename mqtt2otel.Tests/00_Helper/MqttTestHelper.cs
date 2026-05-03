using MQTTnet;
using MQTTnet.Server;


namespace mqtt2otel.Server.Helper
{
    public static class MqttTestHelper
    {
        private static MqttServer? mqttServer;

        private static IMqttClient? mqttClient;

        private static int port = 1883;

        private static readonly SemaphoreSlim mutex = new SemaphoreSlim(1, 1);

        public static async Task EnsureServerIsStarted()
        {
            await mutex.WaitAsync();
            try
            {
                if (mqttServer != null) return;

                var options = new MqttServerOptionsBuilder()
                    .WithDefaultEndpoint()
                    .WithDefaultEndpointPort(port)
                    .Build();
                mqttServer = new MqttServerFactory().CreateMqttServer(options);
                await mqttServer.StartAsync();
                await Task.Delay(100);

                mqttClient = new MqttClientFactory().CreateMqttClient();

                await mqttClient.ConnectAsync(
                       new MqttClientOptionsBuilder()
                          .WithTcpServer("127.0.0.1", port)
                          .Build());

                port += 1;
            }
            finally
            {
                mutex.Release();
            }

            Assert.True(mqttClient.IsConnected);
        }

        public static async Task PublishPayload(string topic, string payload)
        {
            await mutex.WaitAsync();
            try
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
            finally
            {
                mutex.Release();
            }
        }
    }
}
