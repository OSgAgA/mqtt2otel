using MQTTnet;
using MQTTnet.Server;
using System.Text;


namespace mqtt2otel.Server.Helper
{
    public class MqttTestHelper : IDisposable
    {
        private MqttServer? mqttServer;

        private IMqttClient? mqttClient;

        public void Dispose()
        {
            this.mqttClient?.Dispose();
            this.mqttServer?.Dispose();
        }

        public async Task EnsureServerIsStarted()
        {
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

        public async Task PublishPayload(string topic, string payload, List<UserProperty>? userProperties = null)
        {
            if (userProperties == null) userProperties = new();

            if (mqttClient == null)
            {
                throw new Exception($"{nameof(PublishPayload)} can only be called, when mqttClient is set. Please call {nameof(EnsureServerIsStarted)} first.");
            }

            var message = new MqttApplicationMessageBuilder()
                                .WithTopic(topic)
                                .WithPayload(payload)
                                .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce);

            foreach (var property in userProperties)
            {
                ReadOnlyMemory<byte> value = new ReadOnlyMemory<byte>(Encoding.UTF8.GetBytes(property.Value));
                message.WithUserProperty(property.Name, value);
            }
                                
            await mqttClient.PublishAsync(message.Build());
        }
    }
}
