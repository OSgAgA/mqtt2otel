using Microsoft.Extensions.Logging;
using mqtt2otel.Manifest;
using mqtt2otel.Helper;
using mqtt2otel.InternalLogging;
using mqtt2otel.Parser;
using mqtt2otel.Stores;
using mqtt2otel.Transformation;
using MQTTnet;
using MQTTnet.Formatter;
using NCalc.Exceptions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenTelemetry.Metrics;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using mqtt2otel.Interfaces;

namespace mqtt2otel
{
    /// <summary>
    /// Represents the main class for communicating with the mqtt broker.
    /// </summary>
    public class MqttCoordinator : IMqttCoordinator
    {
        /// <summary>
        /// Every subscription will get an id. This counter contains the next id that can be used for 
        /// creating a new subscription. Should be increased afterwards.
        /// </summary>
        private uint subscriptionIdCounter = 1;

        /// <summary>
        /// The mqtt client factory.
        /// </summary>
        private MqttClientFactory mqttFactory = new MqttClientFactory();

        /// <summary>
        /// The created mqttClient as a map, mapping the broker name to ghe corresponding client.
        /// </summary>
        private Dictionary<string, IMqttClient> mqttClient = new();

        /// <summary>
        /// Maps a subscription id to a processor.
        /// </summary>
        private Dictionary<uint, Processor> subscriptionIdProcessorMapping = new();

        /// <summary>
        /// Maps a subscription id to a subscription.
        /// </summary>
        private Dictionary<uint, MqttSubscription> subscriptionIdSubscriptionMapping = new();

        /// <summary>
        /// Used for internal logging.
        /// </summary>
        private ILogger<MqttCoordinator> internalLogger;

        /// <summary>
        /// The settings for connecting to a broker as a map, that maps the broker settings name to the corresponding settings.
        /// </summary>
        private Dictionary<string, MqttBroker> nameToBrokerMap = new();

        /// <summary>
        /// The name of the default broker. 
        /// </summary>
        private string defaultBrokerName = string.Empty;

        /// <summary>
        /// The client id with which this client has been identified at the mqtt broker as a map, that maps the settings name to 
        /// the corresponding client id.
        /// </summary>
        private Dictionary<string, string> clientId = new();

        /// <summary>
        /// The main manifest object.
        /// </summary>
        private Manifest.Manifest manifest = new();

        /// <summary>
        /// Indicates whether the coordiniator is currently disconnecting the brokers.
        /// </summary>
        private bool isDiconnecting = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="MqttCoordinator"/> class.
        /// </summary>
        /// <param name="internalLogger">The logger for internal log messages.</param>
        public MqttCoordinator(ILogger<MqttCoordinator> internalLogger)
        {
            this.internalLogger = internalLogger;
        }

        /// <summary>
        /// Connects to the server and subscribes to all topics as defined in the provided settings.
        /// </summary>
        /// <param name="manifest">The manifest containing information about connection details and subscriptions.</param>
        /// <exception cref="Exception">Thrown if client is unable to connect to server or if manifest contains an error.</exception>
        public async Task ConnectAndSubscribe(Manifest.Manifest manifest)
        {
            this.isDiconnecting = false;

            if (manifest.MqttBroker.Count > 0) this.defaultBrokerName = manifest.MqttBroker[0].Name;

            subscriptionIdCounter = 1;
            this.manifest = manifest;

            foreach (var broker in manifest.MqttBroker)
            {
                await this.ConnectAndSubscribe(broker);
            }

            this.internalLogger.LogInformation($"Subscribing to mqtt events...");
            using (var activity = InternalLogFactory.MainActivitySource.StartActivity("Subscribing to mqtt events"))
            {
                using (var activityMetric = InternalLogFactory.MainActivitySource.StartActivity("Subscribing to metric events"))
                {
                    foreach (var processor in manifest.Processors)
                    {
                        await this.AddProcessorSubscriptions(processor);
                    }
                }
            }
            this.internalLogger.LogInformation("Successfully subscribed to all events.");
        }

        /// <summary>
        /// Disconnects all brokers.
        /// </summary>
        public async Task DisconnectAllBrokers()
        {
            this.isDiconnecting = true;
            this.internalLogger.LogInformation("Disconnecting from all mqtt brokers.");

            using (this.internalLogger.StartActivity("Disconnect from brokers"))
            {
                foreach (var broker in manifest.MqttBroker)
                {
                    try
                    {
                        await this.mqttClient[broker.Name].DisconnectAsync();
                        this.internalLogger.LogInformation($"Disconnected from broker: {broker.Name}");
                    }
                    catch (Exception ex)
                    {
                        this.internalLogger.LogError(ex, $"Could not disconnect from mqtt broker {broker.Name}.");
                    }
                }
            }
        }

        /// <summary>
        /// Connects to a given server and subscribes to all topics as defined in the provided settings.
        /// </summary>
        /// <param name="broker">The broker settings containing information about connection details.</param>
        /// <exception cref="Exception">Thrown if client is unable to connect to server or if settings contain an error.</exception>
        private async Task ConnectAndSubscribe(MqttBroker broker)
        {
            this.nameToBrokerMap[broker.Name] = broker;
            this.clientId[broker.Name] = broker.ClientPrefix + "-" + Guid.NewGuid().ToString();
            this.mqttClient[broker.Name] = this.mqttFactory.CreateMqttClient();


            this.internalLogger.LogInformation($"Connecting to mqtt broker at {broker.Endpoint.FullAddress}...");
            var mqttClientOptionsBuilder = new MqttClientOptionsBuilder();

            if (broker.Endpoint.ConnectionType == MqttBrokerConnectionType.Tcp)
            {
                mqttClientOptionsBuilder.WithTcpServer(broker.Endpoint.Address, port: (int)broker.Endpoint.Port);
            }
            else
            {
                mqttClientOptionsBuilder.WithWebSocketServer(o => o.WithUri(broker.Endpoint.FullAddress));
            }

            if (broker.Endpoint.MqttProtocollVersion != null)
            {
                mqttClientOptionsBuilder.WithProtocolVersion(broker.Endpoint.MqttProtocollVersion.Value);
            }

            if (broker.Endpoint.EnableTls)
            {
                mqttClientOptionsBuilder.WithTlsOptions(
                o =>
                {
                    if (broker.Endpoint.TlsSslProtocol != null) o.WithSslProtocols(broker.Endpoint.TlsSslProtocol.Value);

                    if (broker.Endpoint.TlsCaFilePath != null)
                    {
                        var caChain = new X509Certificate2Collection();
                        caChain.ImportFromPemFile(broker.Endpoint.TlsCaFilePath);
                        o.WithTrustChain(caChain);
                    }

                    o.Build();
                });
            }

            if (!broker.Endpoint.UsePacketFragmentation) mqttClientOptionsBuilder.WithoutPacketFragmentation();

            if (broker.Endpoint.Username != null) mqttClientOptionsBuilder.WithCredentials(broker.Endpoint.Username, broker.Endpoint.Password);

            var mqttClientOptions = mqttClientOptionsBuilder.Build();
            mqttClientOptions.ClientId = this.clientId[broker.Name];

            // Subscribe to server events.
            this.mqttClient[broker.Name].ApplicationMessageReceivedAsync += OnMessageReceived;
            this.mqttClient[broker.Name].DisconnectedAsync += args => OnBrokerDisconnect(args, broker.Name);

            // This will throw an exception if the server is not available.
            // The result from this message returns additional data which was sent
            // from the server. Please refer to the MQTT protocol specification for details.
            var response = await mqttClient[broker.Name].ConnectAsync(mqttClientOptions, CancellationToken.None);

            if (response == null)
            {
                this.internalLogger.LogWarning("Connection to mqtt broker returned null response.");
            }
            else
            {
                using (this.internalLogger.StartActivity("Mqtt broker response"))
                {
                    this.internalLogger.LogInformation("Mqtt broker client id:                  {MqttBrokerResponseInformation}", mqttClientOptions.ClientId);
                    this.internalLogger.LogInformation("Mqtt broker information:                {MqttBrokerResponseInformation}", response.ResponseInformation);
                    this.internalLogger.LogInformation("Mqtt broker reason:                     {MqttBrokerResponseReason}", response.ReasonString);
                    this.internalLogger.LogInformation("Mqtt broker retain available:           {MqttBrokerRetainAvailable}", response.RetainAvailable);
                    this.internalLogger.LogInformation("Mqtt broker maximum QoS:                {MqttBrokerMaxQoS}", response.MaximumQoS);
                    this.internalLogger.LogInformation("Mqtt broker receive max:                {MqttBrokerReceiveMax}", response.ReceiveMaximum);
                    this.internalLogger.LogInformation("Mqtt broker assigned client id:         {MqttBrokerClientId}", response.AssignedClientIdentifier);
                    this.internalLogger.LogInformation("Mqtt broker authentication data:        {MqttBrokerAuthenticationData}", response.AuthenticationData);
                    this.internalLogger.LogInformation("Mqtt broker authentication method:      {MqttBrokerAuthenticationMethod}", response.AuthenticationMethod);
                    this.internalLogger.LogInformation("Mqtt broker session present:            {MqttBrokerIsSessionPresent}", response.IsSessionPresent);
                    this.internalLogger.LogInformation("Mqtt broker maximum packet size:        {MqttBrokerMaximumPacketSize}", response.MaximumPacketSize);
                    this.internalLogger.LogInformation("Mqtt broker result code:                {MqttBrokerResultCode}", response.ResultCode);
                    this.internalLogger.LogInformation("Mqtt broker server keep alive (s):      {MqttBrokerServerKeepAlive}", response.ServerKeepAlive);
                    this.internalLogger.LogInformation("Mqtt broker server reference:           {MqttBrokerServerReference}", response.ServerReference);
                    this.internalLogger.LogInformation("Mqtt broker session expiry interval:    {MqttBrokerSessionExpiryInterval}", response.SessionExpiryInterval);
                    this.internalLogger.LogInformation("Mqtt broker shared sub available:       {MqttBrokerSharedSubscriptionAvailable}", response.SharedSubscriptionAvailable);
                    this.internalLogger.LogInformation("Mqtt broker subscription ids available: {MqttBrokerIdentifierAvailable}", response.SubscriptionIdentifiersAvailable);
                    this.internalLogger.LogInformation("Mqtt broker topic alias max:            {MqttBrokerTopicAliasMaximum}", response.TopicAliasMaximum);
                    this.internalLogger.LogInformation("Mqtt broker wildcard subs available:    {MqttBrokerWildcardSubscriptionAvailable}", response.WildcardSubscriptionAvailable);
                    this.internalLogger.LogInformation("Mqtt broker user properties:            {MqttBrokerUserProperties}", response.UserProperties);
                }
            }

            this.internalLogger.LogInformation($"Successfully connected to mqtt broker.");
        }

        /// <summary>
        /// Called when the connection to the mqtt broker is lost. Tries to reconnect to the broker.
        /// 
        /// Between each reconnet there is a delay, that can be configured via <see cref="MqttBroker.ReconnectDelayInMs"/>.
        /// </summary>
        /// <param name="args">The disonnect event args.</param>
        /// <param name="brokerName">The name of the broker that has been disconnected.</param>
        private async Task OnBrokerDisconnect(MqttClientDisconnectedEventArgs args, string brokerName)
        {
            if (this.isDiconnecting) return;

            this.internalLogger.LogError($"Client ${this.clientId[brokerName]} disconnected from broker: {args.ReasonString}");

            bool connected = false;

            while (!connected)
            {
                this.internalLogger.LogInformation("Trying to reconnect to mqtt broker.");
                try
                {
                    await this.ConnectAndSubscribe(this.manifest);
                    connected = this.mqttClient[brokerName].IsConnected;
                }
                catch
                {
                    connected = false;
                }

                if (!connected)
                {
                    this.internalLogger.LogError($"Could not connect to mqtt broker. Waiting for ${this.nameToBrokerMap[brokerName].ReconnectDelayInMs}ms.");
                    await Task.Delay(this.nameToBrokerMap[brokerName].ReconnectDelayInMs);
                }
                else
                {
                    this.internalLogger.LogInformation("Successfully reconnected to mqtt broker.");
                }
            }
        }

        /// <summary>
        /// Adds all subscription of a given processor to the broker.
        /// </summary>
        /// <param name="processor">The processor containing the subscriptions.</param>
        private async Task AddProcessorSubscriptions(Processor processor)
        {
            foreach (var mqttSubscriptionSettings in processor.Mqtt.Subscriptions)
            {
                await this.SubscribeToTopic(processor, mqttSubscriptionSettings);
            }
        }

        /// <summary>
        /// Subscribes to a topic.
        /// </summary>
        /// <param name="processor">The processor settings defining the topic to subscribe and the rule that should be applied.</param>
        /// <param name="mqttSubscriptionSetting">The settings defining the subscription.</param>
        private async Task SubscribeToTopic(Processor processor, MqttSubscription mqttSubscriptionSetting)
        {
            this.subscriptionIdProcessorMapping[this.subscriptionIdCounter] = processor;
            this.subscriptionIdSubscriptionMapping[this.subscriptionIdCounter] = mqttSubscriptionSetting;
            var mqttSubscribeOptions = mqttFactory.CreateSubscribeOptionsBuilder().WithTopicFilter(mqttSubscriptionSetting.Topic).WithSubscriptionIdentifier(this.subscriptionIdCounter++).Build();
            this.internalLogger.LogInformation($"Subscribed to topic {mqttSubscriptionSetting.Topic}.");

            await this.GetClient(mqttSubscriptionSetting.Broker).SubscribeAsync(mqttSubscribeOptions, CancellationToken.None);
        }

        /// <summary>
        /// Gets the mqtt client for a broker. If name is null, then the default broker is returned.
        /// </summary>
        /// <param name="brokerName">The name of the broker.</param>
        /// <returns></returns>
        private IMqttClient GetClient(string? brokerName)
        {
            if (brokerName == null) return this.mqttClient[this.defaultBrokerName];

            return this.mqttClient[brokerName];
        }

        /// <summary>
        /// Called when a mqtt messag is received for a subscribed topic. Will process the message asynchronously.
        /// </summary>
        /// <param name="e">The message event args.</param>
        private async Task OnMessageReceived(MqttApplicationMessageReceivedEventArgs e)
        {
            try
            {
                bool flowControl = await ProcessReceivedMessage(e);
                if (!flowControl)
                {
                    return;
                }
            }
            catch (ExpressionParsingException ex)
            {
                this.internalLogger.LogError($"{ex.Message}");
            }
            catch (Exception ex)
            {
                this.internalLogger.LogError(ex, $"Could not process message. An internal error occured.");
            }

        }

        /// <summary>
        /// Processes a received mqtt message.
        /// </summary>
        /// <param name="e">The message event args.</param>
        /// <returns>A value indicating whether processing has been successful.</returns>
        private async Task<bool> ProcessReceivedMessage(MqttApplicationMessageReceivedEventArgs e)
        {
            var payload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);

            this.internalLogger.LogDebug($"Message received. Payload: {payload}");

            if (!e.ApplicationMessage.SubscriptionIdentifiers.Any())
            {
                this.internalLogger.LogError("Internal error: Received subscription message without any subscription identifiers.");
                return false;
            }

            var subscriptionId = e.ApplicationMessage.SubscriptionIdentifiers.First<uint>();

            if (!this.subscriptionIdProcessorMapping.ContainsKey(subscriptionId))
            {
                this.internalLogger.LogError($"Internal error while processing received mqtt message: {nameof(subscriptionIdProcessorMapping)} does not contain key {subscriptionId}. Skipping event.");
                return false;
            }

            if (!this.subscriptionIdSubscriptionMapping.ContainsKey(subscriptionId))
            {
                this.internalLogger.LogError($"Internal error while processing received mqtt message: {nameof(subscriptionIdSubscriptionMapping)} does not contain key {subscriptionId}. Skipping event.");
                return false;
            }

            var processor = this.subscriptionIdProcessorMapping[subscriptionId];
            var subscription = this.subscriptionIdSubscriptionMapping[subscriptionId];

            bool success = false;

            success = await processor.ProcessSubscriptionPayload(payload, subscription);

            if (!success) this.internalLogger.LogError($"Could not process message. See previous errors. Message skipped. Payload: {payload}");
            return true;
        }
    }
}
