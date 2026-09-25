using MQTTnet;
using MQTTnet.Server;
using System.Text;


namespace mqtt2otel.Server.Helper
{
    /// <summary>
    /// Responsible for building up a mqtt broker and a client to simulate a message flow.
    /// </summary>
    public class MqttTestHelper : IDisposable
    {
        /// <summary>
        /// The mqtt broker.
        /// </summary>
        private MqttServer? mqttServer;

        /// <summary>
        /// A client for communicating with the broker.
        /// </summary>
        private IMqttClient? mqttClient;

        /// <summary>
        /// Disposes the object.
        /// </summary>
        public void Dispose()
        {
            this.mqttClient?.Dispose();
            this.mqttServer?.Dispose();
        }

        /// <summary>
        /// Ensures the server is started. Will return directly, if server is already running.
        /// </summary>
        /// <returns></returns>
        public async Task EnsureServerIsStarted()
        {
            if (this.mqttServer != null) return;

            var options = new MqttServerOptionsBuilder()
                .WithDefaultEndpoint()
                .WithDefaultEndpointPort(1883)
                .Build();
            mqttServer = new MqttServerFactory().CreateMqttServer(options);
            await mqttServer.StartAsync();
            await Task.Delay(100);
        }

        /// <summary>
        /// Connects a client to the server.
        /// </summary>
        public async Task ConnectClient()
        {
            mqttClient = new MqttClientFactory().CreateMqttClient();

            await mqttClient.ConnectAsync(
                   new MqttClientOptionsBuilder()
                      .WithTcpServer("127.0.0.1", 1883)
                      .Build());

            Assert.True(mqttClient.IsConnected);
        }

        /// <summary>
        /// Disconnects the client from the server.
        /// </summary>
        public async Task DisconnectClient()
        {
            if (this.mqttClient == null) return;

            await this.mqttClient.DisconnectAsync();
            this.mqttClient.Dispose();
        }

        /// <summary>
        /// Publishes a payload to the broker.
        /// </summary>
        /// <param name="topic">The message topic-</param>
        /// <param name="payload">The message payload.</param>
        /// <param name="userProperties">The message user properties.</param>
        /// <exception cref="Exception">Thrown when client is not connected.</exception>
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
