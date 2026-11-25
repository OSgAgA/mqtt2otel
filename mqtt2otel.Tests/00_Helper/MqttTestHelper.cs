using mqtt2otel.Metadata;
using MQTTnet;
using MQTTnet.Server;
using System.Data;
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
        /// Sends a message to the broker.
        /// </summary>
        /// <param name="data">The setup data</param>
        /// <exception cref="Exception">Thrown when client is not connected.</exception>
        public async Task SendMessage(MqttSetupData data)
        {
            if (mqttClient == null)
            {
                throw new Exception($"{nameof(SendMessage)} can only be called, when mqttClient is set. Please call {nameof(EnsureServerIsStarted)} first.");
            }

            var message = new MqttApplicationMessageBuilder()
                                .WithTopic(data.Topic)
                                .WithPayload(data.Payload)
                                .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.ExactlyOnce);

            if (!string.IsNullOrEmpty(data.ResponseTopic)) message.WithResponseTopic(data.ResponseTopic);
            if (!string.IsNullOrEmpty(data.CorrelationData)) message.WithCorrelationData(System.Text.Encoding.UTF8.GetBytes(data.CorrelationData));

            foreach (var property in data.UserProperties)
            {
                ReadOnlyMemory<byte> value = new ReadOnlyMemory<byte>(Encoding.UTF8.GetBytes(property.Value));
                message.WithUserProperty(property.Name, value);
            }
                                
            await mqttClient.PublishAsync(message.Build());
        }
    }
}
