using Microsoft.Extensions.Logging;
using mqtt2otel.Configuration;
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

namespace mqtt2otel
{
    /// <summary>
    /// Represents the main class for communicating with the mqtt broker.
    /// </summary>
    public class MqttCoordinator
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
        /// Maps a subscription id to a subscription to which a metric rule should be applied.
        /// </summary>
        private Dictionary<uint, MqttSubscriptionContext<MetricsRuleSettings>> subscriptionMetricsMapping = new();

        /// <summary>
        /// Maps a subscription id to a subscription to which a log rule should be applied.
        /// </summary>
        private Dictionary<uint, MqttSubscriptionContext<LoggingRuleSettings>> subscriptionLogsMapping = new();

        /// <summary>
        /// Maps a subscription id to a subscription type.
        /// </summary>
        private Dictionary<uint, SubscriptionType> subscriptionTypeMapping = new Dictionary<uint, SubscriptionType>();

        /// <summary>
        /// The signal store used for storing metric signals.
        /// </summary>
        private SignalStore signalStore;

        /// <summary>
        /// The logger store used for getting open telemetry loggers.
        /// </summary>
        private LoggerStore loggerStore;

        /// <summary>
        /// The payload parser that will be used to parse mqtt subscription payloads.
        /// </summary>
        private PayloadParser payloadParser;

        /// <summary>
        /// The payload transformation parser that will be used to transform mqtt subscription payloads.
        /// </summary>
        private PayloadTransformation payloadTransformation;

        /// <summary>
        /// Used for internal logging.
        /// </summary>
        private ILogger<MqttCoordinator> internalLogger;

        /// <summary>
        /// The settings for connecting to a broker as a map, that maps the broker settings name to the corresponding settings.
        /// </summary>
        private Dictionary<string, MqttBrokerSettings> brokerSettings = new();

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
        /// The main settings object.
        /// </summary>
        private Manifest settings = new();

        /// <summary>
        /// Indicates whether the coordiniator is currently disconnecting the brokers.
        /// </summary>
        private bool isDiconnecting = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="MqttCoordinator"/> class.
        /// </summary>
        /// <param name="internalLogger">The logger for internal log messages.</param>
        /// <param name="store">The signal store to store metric signals.</param>
        /// <param name="loggerStore">The logger store to access open telemetry loggers.</param>
        /// <param name="payloadParser">The payload parser to be used.</param>
        /// <param name="payloadTransformation">The payload transformer.</param>
        public MqttCoordinator(ILogger<MqttCoordinator> internalLogger, SignalStore store, LoggerStore loggerStore, PayloadParser payloadParser, PayloadTransformation payloadTransformation)
        {
            this.internalLogger = internalLogger;
            this.signalStore = store;
            this.loggerStore = loggerStore;
            this.payloadParser = payloadParser;
            this.payloadTransformation = payloadTransformation;
        }

        /// <summary>
        /// Connects to the server and subscribes to all topics as defined in the provided settings.
        /// </summary>
        /// <param name="settings">The settings containing information about connection details and subscriptions.</param>
        /// <exception cref="Exception">Thrown if client is unable to connect to server or if settings contain an error.</exception>
        public async Task ConnectAndSubscribe(Manifest settings)
        {
            this.isDiconnecting = false;

            if (settings.MqttBroker.Count > 0) this.defaultBrokerName = settings.MqttBroker[0].Name;

            subscriptionIdCounter = 1;
            subscriptionMetricsMapping = new();
            subscriptionLogsMapping = new();
            subscriptionTypeMapping = new();
            this.settings = settings;

            foreach (var broker in settings.MqttBroker)
            {
                await this.ConnectAndSubscribe(broker);
            }

            this.internalLogger.LogInformation($"Subscribing to mqtt events...");
            using (var activity = InternalLogFactory.MainActivitySource.StartActivity("Subscribing to mqtt events"))
            {
                this.internalLogger.LogInformation($"Subscribing to metric events...");
                using (var activityMetric = InternalLogFactory.MainActivitySource.StartActivity("Subscribing to metric events"))
                {
                    await this.AddMetricsSubscriptions(settings.Metrics);
                }

                this.internalLogger.LogInformation($"Subscribing to log events...");
                using (var activityLogs = InternalLogFactory.MainActivitySource.StartActivity("Subscribing to log events"))
                {
                    await this.AddLoggingSubscriptions(settings.Logs);
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
                foreach (var brokerSettings in settings.MqttBroker)
                {
                    try
                    {
                        await this.mqttClient[brokerSettings.Name].DisconnectAsync();
                        this.internalLogger.LogInformation($"Disconnected from broker: {brokerSettings.Name}");
                    }
                    catch (Exception ex)
                    {
                        this.internalLogger.LogError(ex, $"Could not disconnect from mqtt broker {brokerSettings.Name}.");
                    }
                }
            }
        }

        /// <summary>
        /// Connects to a given server and subscribes to all topics as defined in the provided settings.
        /// </summary>
        /// <param name="brokerSettings">The broker settings containing information about connection details.</param>
        /// <exception cref="Exception">Thrown if client is unable to connect to server or if settings contain an error.</exception>
        private async Task ConnectAndSubscribe(MqttBrokerSettings brokerSettings)
        {
            this.brokerSettings[brokerSettings.Name] = brokerSettings;
            this.clientId[brokerSettings.Name] = brokerSettings.ClientPrefix + "-" + Guid.NewGuid().ToString();
            this.mqttClient[brokerSettings.Name] = this.mqttFactory.CreateMqttClient();


            this.internalLogger.LogInformation($"Connecting to mqtt broker at {brokerSettings.Endpoint.FullAddress}...");
            var mqttClientOptionsBuilder = new MqttClientOptionsBuilder();

            if (brokerSettings.Endpoint.ConnectionType == MqttBrokerConnectionType.Tcp)
            {
                mqttClientOptionsBuilder.WithTcpServer(brokerSettings.Endpoint.Address, port: (int)brokerSettings.Endpoint.Port);
            }
            else
            {
                mqttClientOptionsBuilder.WithWebSocketServer(o => o.WithUri(brokerSettings.Endpoint.FullAddress));
            }

            if (brokerSettings.Endpoint.MqttProtocollVersion != null)
            {
                mqttClientOptionsBuilder.WithProtocolVersion(brokerSettings.Endpoint.MqttProtocollVersion.Value);
            }

            if (brokerSettings.Endpoint.EnableTls)
            {
                mqttClientOptionsBuilder.WithTlsOptions(
                o =>
                {
                    if (brokerSettings.Endpoint.TlsSslProtocol != null) o.WithSslProtocols(brokerSettings.Endpoint.TlsSslProtocol.Value);

                    if (brokerSettings.Endpoint.TlsCaFilePath != null)
                    {
                        var caChain = new X509Certificate2Collection();
                        caChain.ImportFromPemFile(brokerSettings.Endpoint.TlsCaFilePath);
                        o.WithTrustChain(caChain);
                    }

                    o.Build();
                });
            }

            if (!brokerSettings.Endpoint.UsePacketFragmentation) mqttClientOptionsBuilder.WithoutPacketFragmentation();

            if (brokerSettings.Endpoint.Username != null) mqttClientOptionsBuilder.WithCredentials(brokerSettings.Endpoint.Username, brokerSettings.Endpoint.Password);

            var mqttClientOptions = mqttClientOptionsBuilder.Build();
            mqttClientOptions.ClientId = this.clientId[brokerSettings.Name];

            // Subscribe to server events.
            this.mqttClient[brokerSettings.Name].ApplicationMessageReceivedAsync += OnMessageReceived;
            this.mqttClient[brokerSettings.Name].DisconnectedAsync += args => OnBrokerDisconnect(args, brokerSettings.Name);

            // This will throw an exception if the server is not available.
            // The result from this message returns additional data which was sent
            // from the server. Please refer to the MQTT protocol specification for details.
            var response = await mqttClient[brokerSettings.Name].ConnectAsync(mqttClientOptions, CancellationToken.None);

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
        /// Between each reconnet there is a delay, that can be configured via <see cref="MqttBrokerSettings.ReconnectDelayInMs"/>.
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
                    await this.ConnectAndSubscribe(this.settings);
                    connected = this.mqttClient[brokerName].IsConnected;
                }
                catch
                {
                    connected = false;
                }

                if (!connected)
                {
                    this.internalLogger.LogError($"Could not connect to mqtt broker. Waiting for ${this.brokerSettings[brokerName].ReconnectDelayInMs}ms.");
                    await Task.Delay(this.brokerSettings[brokerName].ReconnectDelayInMs);
                }
                else
                {
                    this.internalLogger.LogInformation("Successfully reconnected to mqtt broker.");
                }
            }
        }

        /// <summary>
        /// Adds all metric subscription to the broker.
        /// </summary>
        /// <param name="metricRules">The settings that define the metric subscriptions.</param>
        private async Task AddMetricsSubscriptions(List<MetricsRuleSettings> metricRules)
        {
            foreach (var metricRule in metricRules)
            {
                foreach (var mqttSubscriptionSettings in metricRule.Mqtt.Subscriptions)
                {
                    await this.SubscribeToTopic(metricRule, mqttSubscriptionSettings, this.subscriptionMetricsMapping, SubscriptionType.Metric);
                }
            }
        }

        /// <summary>
        /// Adds all loging subscription to the broker.
        /// </summary>
        /// <param name="settings">The settings that define the logging subscriptions.</param>
        /// <returns></returns>
        private async Task AddLoggingSubscriptions(List<LoggingRuleSettings> logRules)
        {
            foreach (var logRule in logRules)
            {
                foreach (var mqttSubscriptionSettings in logRule.Mqtt.Subscriptions)
                {
                    await this.SubscribeToTopic(logRule, mqttSubscriptionSettings, this.subscriptionLogsMapping, SubscriptionType.Log);
                }
            }
        }

        /// <summary>
        /// Subscribes to a topic.
        /// </summary>
        /// <typeparam name="TSettings">The type of the provided settings.</typeparam>
        /// <param name="settings">The settings defining the topic to subscribe and the rule that should be applied.</param>
        /// <param name="mqttSubscriptionSetting">The settings defining the subscription.</param>
        /// <param name="subscriptionMapping">A dictionary that will map a subscription id to a <see cref="MqttSubscriptionContext{TSettings}"/>.</param>
        /// <param name="type">The type of the subscription. See <see cref="SubscriptionType"/></param>
        private async Task SubscribeToTopic<TSettings>(TSettings settings, MqttSubscriptionSettings mqttSubscriptionSetting, Dictionary<uint, MqttSubscriptionContext<TSettings>> subscriptionMapping, SubscriptionType type)
        {
            subscriptionMapping[this.subscriptionIdCounter] = new MqttSubscriptionContext<TSettings>(settings, mqttSubscriptionSetting);
            this.subscriptionTypeMapping[this.subscriptionIdCounter] = type;
            var mqttSubscribeOptions = mqttFactory.CreateSubscribeOptionsBuilder().WithTopicFilter(mqttSubscriptionSetting.Topic).WithSubscriptionIdentifier(this.subscriptionIdCounter++).Build();
            this.internalLogger.LogInformation($"Subscribed to topic {mqttSubscriptionSetting.Topic}.");

            var result = await this.GetClient(mqttSubscriptionSetting.Broker).SubscribeAsync(mqttSubscribeOptions, CancellationToken.None);
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

            if (!this.subscriptionTypeMapping.ContainsKey(subscriptionId))
            {
                this.internalLogger.LogError($"Internal error: {nameof(subscriptionTypeMapping)} does not contain key {subscriptionId}. Skipping event.");
                return false;
            }

            bool success = false;

            switch (this.subscriptionTypeMapping[subscriptionId])
            {
                case SubscriptionType.Log:
                    success = await this.ProcessLogsSubscription(payload, subscriptionId);
                    break;
                case SubscriptionType.Metric:
                    success = await this.ProcessMetricsSubscription(payload, subscriptionId);
                    break;
            }

            if (!success) this.internalLogger.LogError($"Could not process message. See previous errors. Message skipped. Payload: {payload}");
            return true;
        }

        /// <summary>
        /// Process a subscription message that has been identified as a metric rule.
        /// </summary>
        /// <param name="payload">The message payload.</param>
        /// <param name="subscriptionId">The subscription id.</param>
        /// <returns>A value indicating whether processing has been successful.</returns>
        private async Task<bool> ProcessMetricsSubscription(string payload, uint subscriptionId)
        {
            if (!this.subscriptionMetricsMapping.ContainsKey(subscriptionId))
            {
                this.internalLogger.LogError($"Internal error: {nameof(subscriptionMetricsMapping)} does not contain key {subscriptionId}. Skipping event.");
                return false;
            }

            var context = this.subscriptionMetricsMapping[subscriptionId];

            if (context.MqttSubscriptionSettings.Transform != null)
            {
                payload = await this.payloadTransformation.Apply(SubscriptionType.Metric, context.MqttSubscriptionSettings.Name, payload, context.MqttSubscriptionSettings.Transform);
            }

            foreach (var ruleSettings in context.Settings.Otel.Rules)
            {
                if (ruleSettings.Name == null) continue;
                var key = context.MqttSubscriptionSettings.Id + ":" + ruleSettings.Id;
                var combinedVariables = context.Settings.Mqtt.Variables.Combine(context.MqttSubscriptionSettings.Variables);
                await this.WriteValueToSignalStore(key, context.Settings.Otel, ruleSettings, payload, combinedVariables);
            }

            return true;
        }

        /// <summary>
        /// Process a subscription message that has been identified as a logging rule.
        /// </summary>
        /// <param name="payload">The message payload.</param>
        /// <param name="subscriptionId">The subscription id.</param>
        /// <returns>A value indicating whether processing has been successful.</returns>
        private async Task<bool> ProcessLogsSubscription(string payload, uint subscriptionId)
        {
            if (!this.subscriptionLogsMapping.ContainsKey(subscriptionId))
            {
                this.internalLogger.LogError($"Internal error: {nameof(subscriptionLogsMapping)} does not contain key {subscriptionId}. Skipping event.");
                return false;
            }

            var context = this.subscriptionLogsMapping[subscriptionId];
            var loggingSettings = context.Settings;
            var mqttSettings = context.MqttSubscriptionSettings;

            if (mqttSettings.Transform != null)
            {
                payload = await this.payloadTransformation.Apply(SubscriptionType.Log, loggingSettings.Name, payload, mqttSettings.Transform);
            }

            bool success = true;

            foreach (var otelSettings in loggingSettings.Otel.Rules)
            {
                var key = otelSettings.Id;
                if (!this.loggerStore.ContainsKey(key))
                {
                    this.internalLogger.LogError($"Internal error: Could not get logger with id: {key}. Skipping event.");
                    return false;
                }

                var logger = this.loggerStore.GetLogger(key);

                success = await logger.ProcessLogMessage(payload, loggingSettings, mqttSettings.Variables, this.internalLogger);
            }

            return success;
        }

        /// <summary>
        /// Stores a metric signal in the signal store.
        /// </summary>
        /// <param name="key">The key under which the object should be stored.</param>
        /// <param name="otelSettings">The otel settings that should be used to process this signal.</param>
        /// <param name="ruleSettings">The otel metric rule settings that should be used to process this signal.</param>
        /// <param name="payload">The payload that should be processed.</param>
        /// <param name="variables">The variables that can be applied to the payload.</param>
        /// <returns></returns>
        private async Task WriteValueToSignalStore(string key, OtelMetricSettings otelSettings, OtelMetricRuleSettings ruleSettings, string payload, IEnumerable<Variable> variables)
        {
            if (ruleSettings.Name == null) return;

            var combinedAttributes = otelSettings.Attributes.Combine(ruleSettings.Attributes);
            IEnumerable<Variable> expandedAttributes = VariableParser.Expand(combinedAttributes, variables);

            try
            {
                switch (ruleSettings.SignalDataType)
                {
                    case SignalDataType.Float:
                        await UpdateSignalStoreValue<float>(key, ruleSettings, payload, expandedAttributes);
                        break;
                    case SignalDataType.Int:
                        await UpdateSignalStoreValue<int>(key, ruleSettings, payload, expandedAttributes);
                        break;
                    case SignalDataType.Double:
                        await UpdateSignalStoreValue<double>(key, ruleSettings, payload, expandedAttributes);
                        break;
                    case SignalDataType.Long:
                        await UpdateSignalStoreValue<long>(key, ruleSettings, payload, expandedAttributes);
                        break;
                    case SignalDataType.Decimal:
                        await UpdateSignalStoreValue<decimal>(key, ruleSettings, payload, expandedAttributes);
                        break;
                    case SignalDataType.String:
                        await UpdateSignalStoreValue<string>(key, ruleSettings, payload, expandedAttributes);
                        break;
                    case SignalDataType.DateTime:
                        await UpdateSignalStoreValue<DateTime>(key, ruleSettings, payload, expandedAttributes);
                        break;
                    default:
                        throw new ExpressionParsingException(new Exception(), SubscriptionType.Metric, ruleSettings.Name, $"Signal type {ruleSettings.SignalDataType} not supported.");
                }
            }
            catch (ExpressionParsingException ex)
            {
                this.internalLogger.LogError($"{ex.Message}");
            }
            catch (Exception ex)
            {
                this.internalLogger.LogError(ex, $"Internal error. Could not write signal to metricsContainer.");
            }
        }

        /// <summary>
        /// Updates a value in the signal store.
        /// </summary>
        /// <typeparam name="T">The type of the value inside the store.</typeparam>
        /// <param name="key">The key under which the value was stored.</param>
        /// <param name="ruleSettings">The otel metric rule that should be applied.</param>
        /// <param name="payload">The payload to be parsed.</param>
        /// <param name="expandedAttributes">The attributes to be applied to the value.</param>
        /// <returns></returns>
        private async Task UpdateSignalStoreValue<T>(string key, OtelMetricRuleSettings ruleSettings, string payload, IEnumerable<Variable> expandedAttributes)
        {
            T value = await this.payloadParser.Parse<T>(SubscriptionType.Metric, ruleSettings.Name, payload, ruleSettings.Value);
            this.signalStore.UpdateValue(key, value, expandedAttributes);
        }
    }
}
