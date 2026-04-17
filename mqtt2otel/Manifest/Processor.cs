using Microsoft.Extensions.Logging;
using mqtt2otel.Helper;
using mqtt2otel.Interfaces;
using mqtt2otel.Parser;
using mqtt2otel.Stores;
using mqtt2otel.Transformation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace mqtt2otel.Manifest
{
    /// <summary>
    /// Represents a processor. A processor is responsible for subscribing to mqtt topics and
    /// applying otel rules to these subscriptions.
    /// </summary>
    public class Processor : NamedIdObject, IProcessor
    {
        /// <summary>
        /// The data stores used by the application to exchange data asynchronously.
        /// </summary>
        private IDataStores dataStores;


        /// <summary>
        /// The logger used internaly for logging.
        /// </summary>
        private ILogger internalLogger;

        /// <summary>
        /// The payload parser for processing payloads.
        /// </summary>
        private IPayloadParser payloadParser;

        /// <summary>
        /// The object used for processing payload transformations.
        /// </summary>
        private IPayloadTransformation payloadTransformation;

        /// <summary>
        /// Creates a new instance of the <see cref="Processor"/> type.
        /// </summary>
        /// <param name="internalLogger">The logger used internaly for logging.</param>
        /// <param name="payloadParser">The payload parser for processing payloads.</param>
        /// <param name="payloadTransformation">The object used for processing payload transformations.</param>
        /// <param name="dataStores">The data stores used by the application to exchange data asynchronously.</param>
        public Processor(ILogger internalLogger, IPayloadParser payloadParser, IPayloadTransformation payloadTransformation, IDataStores dataStores)
        {
            this.internalLogger = internalLogger;
            this.payloadParser = payloadParser;
            this.payloadTransformation = payloadTransformation;
            this.dataStores = dataStores;
        }

        /// <summary>
        /// Gets or sets the otel settings for the rule.
        /// </summary>
        public Otel Otel { get; set; } = new();

        /// <summary>
        /// Gets or sets the mqtt settings for the rule.
        /// </summary>
        public Mqtt Mqtt { get; set; } = new();

        /// <summary>
        /// Gets or sets the name of the open telemetriy server to be used for all rules in this section. 
        /// Set to null for using the default server.
        /// </summary>
        public string? OtelServerName { get; set; } = null;

        /// <summary>
        /// Validates the object.
        /// </summary>
        /// <param name="result">The validation result.</param>
        public void Validate(ValidationResult result)
        {
            string context = $"Metric ({this.Name})";
            this.Otel.Validate(context, result);
            this.Mqtt.Validate(context, result);
        }

        /// <summary>
        /// Process a subscription payload that was received from the mqtt broker.
        /// </summary>
        /// <param name="payload">The received payload.</param>
        /// <param name="subscription">The subscription that received the payload.</param>
        /// <returns>A value indicating whether the operation has been successful.</returns>
        public async Task<bool> ProcessSubscriptionPayload(string payload, MqttSubscription subscription)
        {
            bool success = await this.ProcessMetricsSubscription(payload, subscription);
            success = success && await this.ProcessLogsSubscription(payload, subscription);

            return success;
        }

        /// <summary>
        /// Process a subscription message by applying all metric rules..
        /// </summary>
        /// <param name="payload">The message payload.</param>
        /// <param name="subscription">The settings of the subscription that triggered this processor.</param>
        /// <returns>A value indicating whether processing has been successful.</returns>
        private async Task<bool> ProcessMetricsSubscription(string payload, MqttSubscription subscription)
        {

            foreach (var ruleSettings in this.Otel.Metrics)
            {
                if (ruleSettings.Name == null) continue;
                var key = subscription.Id + ":" + ruleSettings.Id;
                var combinedVariables = this.Mqtt.Variables.Combine(subscription.Variables);
                await this.WriteValueToSignalStore(subscription.Id, ruleSettings.Id, this.Otel, ruleSettings, payload, combinedVariables);
            }

            return true;
        }

        /// <summary>
        /// Process a subscription message that has been identified as a logging rule.
        /// </summary>
        /// <param name="payload">The message payload.</param>
        /// <param name="subscriptionId">The subscription id.</param>
        /// <returns>A value indicating whether processing has been successful.</returns>
        private async Task<bool> ProcessLogsSubscription(string payload, MqttSubscription subscription)
        {
            if (subscription.Transform != null)
            {
                payload = await this.payloadTransformation.Apply(this.Name, payload, subscription.Transform);
            }

            bool success = true;

            foreach (var logRuleSettings in this.Otel.Logs)
            {
                var key = logRuleSettings.Id;
                if (!this.dataStores.LoggerStore.ContainsKey(key))
                {
                    this.internalLogger.LogError($"Internal error: Could not get logger with id: {key}. Skipping event.");
                    return false;
                }

                var logger = this.dataStores.LoggerStore.GetLogger(key);
                var combinedAttributes = logRuleSettings.Attributes.Combine(this.Otel.Attributes);

                success = await logger.ProcessLogMessage(payload, logRuleSettings, subscription.Variables, this.internalLogger, combinedAttributes);
            }

            return success;
        }

        /// <summary>
        /// Stores a metric signal in the signal store.
        /// </summary>
        /// <param name="subscriptionId">The id of the subscription that generated the message from which the signal is received.</param>
        /// <param name="ruleId">The id of the rule, that generated the message from which the signal is received.</param>
        /// <param name="otelSettings">The otel settings that should be used to process this signal.</param>
        /// <param name="rule">The otel metric rule settings that should be used to process this signal.</param>
        /// <param name="payload">The payload that should be processed.</param>
        /// <param name="variables">The variables that can be applied to the payload.</param>
        /// <returns></returns>
        private async Task WriteValueToSignalStore(Guid subscriptionId, Guid ruleId, Otel otelSettings, OtelMetricRule rule, string payload, IEnumerable<Variable> variables)
        {
            if (rule.Name == null) return;

            var combinedAttributes = otelSettings.Attributes.Combine(rule.Attributes);
            IEnumerable<Variable> expandedAttributes = VariableParser.Expand(combinedAttributes, variables);

            try
            {
                switch (rule.SignalDataType)
                {
                    case SignalDataType.Float:
                        await UpdateSignalStoreValue<float>(subscriptionId, ruleId, rule, payload, expandedAttributes);
                        break;
                    case SignalDataType.Int:
                        await UpdateSignalStoreValue<int>(subscriptionId, ruleId, rule, payload, expandedAttributes);
                        break;
                    case SignalDataType.Double:
                        await UpdateSignalStoreValue<double>(subscriptionId, ruleId, rule, payload, expandedAttributes);
                        break;
                    case SignalDataType.Long:
                        await UpdateSignalStoreValue<long>(subscriptionId, ruleId, rule, payload, expandedAttributes);
                        break;
                    case SignalDataType.Decimal:
                        await UpdateSignalStoreValue<decimal>(subscriptionId, ruleId, rule, payload, expandedAttributes);
                        break;
                    case SignalDataType.String:
                        await UpdateSignalStoreValue<string>(subscriptionId, ruleId, rule, payload, expandedAttributes);
                        break;
                    case SignalDataType.DateTime:
                        await UpdateSignalStoreValue<DateTime>(subscriptionId, ruleId, rule, payload, expandedAttributes);
                        break;
                    default:
                        throw new ExpressionParsingException(new Exception(), rule.Name, $"Signal type {rule.SignalDataType} not supported.");
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
        /// <param name="subscriptionId">The id of the subscription that generated the message from which the signal is received.</param>
        /// <param name="ruleId">The id of the rule, that generated the message from which the signal is received.</param>
        /// <param name="rule">The otel metric rule that should be applied.</param>
        /// <param name="payload">The payload to be parsed.</param>
        /// <param name="expandedAttributes">The attributes to be applied to the value.</param>
        /// <returns></returns>
        private async Task UpdateSignalStoreValue<T>(Guid subscriptionId, Guid ruleId, OtelMetricRule rule, string payload, IEnumerable<Variable> expandedAttributes)
        {
            T value = await this.payloadParser.Parse<T>(rule.Name, payload, rule.Value);
            this.dataStores.SignalStore.UpdateValue(subscriptionId, ruleId, value, expandedAttributes);
        }

    }
}