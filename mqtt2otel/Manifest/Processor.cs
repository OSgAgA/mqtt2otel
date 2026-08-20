using Microsoft.Extensions.Logging;
using mqtt2otel.Helper;
using mqtt2otel.Interfaces;
using mqtt2otel.InternalLogging;
using mqtt2otel.InternalMetrics;
using mqtt2otel.Parser;
using mqtt2otel.Stores;
using mqtt2otel.Transformation;
using System;
using System.Collections.Generic;
using System.Data;
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
        /// The meter for recording internal metrics.
        /// </summary>
        private ProcessorMeter processorMeter;

        /// <summary>
        /// Creates a new instance of the <see cref="Processor"/> type.
        /// </summary>
        /// <param name="internalLogger">The logger used internaly for logging.</param>
        /// <param name="payloadParser">The payload parser for processing payloads.</param>
        /// <param name="payloadTransformation">The object used for processing payload transformations.</param>
        /// <param name="dataStores">The data stores used by the application to exchange data asynchronously.</param>
        /// <param name="meter">The meter for recording internal metrics.</param>
        public Processor(ILogger internalLogger, IPayloadParser payloadParser, IPayloadTransformation payloadTransformation, IDataStores dataStores, ProcessorMeter meter)
        {
            this.processorMeter = meter;
            this.internalLogger = internalLogger;
            this.payloadParser = payloadParser;
            this.payloadTransformation = payloadTransformation;
            this.dataStores = dataStores;
        }

        /// <summary>
        /// Gets or sets a value indicating, whether attributes should be created from mqtt user properties (true), or not (false), or
        /// if the default setting should be used (null).
        /// </summary>
        [InheritedProperty]
        public bool? CreateAttributesFromUserProperties { get; set; } = null;

        /// <summary>
        /// Gets or sets the otel settings for the processor.
        /// </summary>
        public Otel Otel { get; set; } = new();

        /// <summary>
        /// Gets or sets the mqtt settings for the processor.
        /// </summary>
        public Mqtt Mqtt { get; set; } = new();

        /// <summary>
        /// Gets or sets the name of the open telemetriy connection to be used for all processors in this section. 
        /// Set to null for using the default connection.
        /// </summary>
        [InheritedProperty]
        public string? OtelConnection { get; set; } = null;

        /// <summary>
        /// Validates the object.
        /// </summary>
        /// <param name="result">The validation result.</param>
        public void Validate(ValidationResult result)
        {
            string context = $"Processor ({this.Name})";
            this.Otel.Validate(context, result);
            this.Mqtt.Validate(context, result);
        }

        /// <summary>
        /// Process a subscription payload that was received from the mqtt broker.
        /// </summary>
        /// <param name="message">The received message.</param>
        /// <param name="subscription">The subscription that received the payload.</param>
        /// <returns>A value indicating whether the operation has been successful.</returns>
        public bool ProcessSubscriptionPayload(MqttMessage message, MqttSubscription subscription)
        {
            bool success = false;

            var tags = new TagList();
            tags.Add("subscription.name", subscription.Name);
            tags.Add("subscription.id", subscription.Id);
            tags.Add("subscription.connection", subscription.BrokerConnection);
            tags.Add("processor.name", this.Name);
            tags.Add("processor.id", this.Id);
            tags.Add("processor.otel.connection", this.OtelConnection);

            var sw = new Stopwatch();
            sw.Start();
            try
            {                
                using (this.internalLogger.StartActivity("Process metrics processors"))
                {
                    success = this.ProcessMetricsSubscription(message, subscription);
                }
                using (this.internalLogger.StartActivity("Process log processors"))
                {
                    success = success && this.ProcessLogsSubscription(message, subscription);
                }
            }
            catch
            {
                sw.Stop();
                this.processorMeter.ProcessingErrorCount.Add(1, tags);
                throw;
            }
            sw.Stop();

            this.processorMeter.ProcessingTimeTotal.Record(sw.ElapsedMicroseconds, tags);

            if (!success) this.processorMeter.ProcessingErrorCount.Add(1, tags);

            return success;
        }

        /// <summary>
        /// Process a subscription message by applying all metric rules..
        /// </summary>
        /// <param name="message">The received message.</param>
        /// <param name="subscription">The settings of the subscription that triggered this processor.</param>
        /// <returns>A value indicating whether processing has been successful.</returns>
        private bool ProcessMetricsSubscription(MqttMessage message, MqttSubscription subscription)
        {

            foreach (var rule in this.Otel.Metrics)
            {
                using (this.internalLogger.StartActivity("Process metrics rule"))
                {
                    if (rule.Name == null) continue;

                    var sw = new Stopwatch();
                    sw.Start();
                    var key = subscription.Id + ":" + rule.Id;
                    var combinedVariables = this.Mqtt.Variables.Combine(subscription.Variables);
                    this.WriteValueToSignalStore(subscription.Id, rule.Id, this.Otel, rule, message, combinedVariables);
                    sw.Stop();

                    var tags = new TagList();
                    tags.Add("processor.name", this.Name);
                    tags.Add("processor.id", this.Id);
                    tags.Add("processor.otel.connection", this.OtelConnection);
                    tags.Add("subscription.name", subscription.Name);
                    tags.Add("subscription.id", subscription.Id);
                    tags.Add("subscription.connection", subscription.BrokerConnection);
                    tags.Add("rule.name", rule.Name);
                    tags.Add("rule.id", rule.Id);
                    tags.Add("rule.connection", rule.OtelConnection);
                    tags.Add("rule.instrument", rule.Instrument);
                    this.processorMeter.ProcessingTimeMetricRule.Record(sw.ElapsedMicroseconds, tags);
                    this.processorMeter.Metrics.Add(1, tags);
                }
            }

            return true;
        }

        /// <summary>
        /// Process a subscription message that has been identified as a logging rule.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="subscription">The subscription that triggered the event.</param>
        /// <returns>A value indicating whether processing has been successful.</returns>
        private bool ProcessLogsSubscription(MqttMessage message, MqttSubscription subscription)
        {
            var swTransform = new Stopwatch();

            using (this.internalLogger.StartActivity("Transform payload"))
            {
                swTransform.Start();
                if (!string.IsNullOrWhiteSpace(subscription.Transform))
                {
                    var combinedVariables = this.Mqtt.Variables.Combine(subscription.Variables);
                    message.Payload = this.payloadTransformation.Apply(this.Name, subscription.Transform, new ParsingContext(combinedVariables, message));
                }
                swTransform.Stop();
            }
            bool success = true;

            foreach (var rule in this.Otel.Logs)
            {
                using (this.internalLogger.StartActivity("Process log rule"))
                {
                    var sw = new Stopwatch();
                    sw.Start();

                    var key = rule.Id;
                    if (!this.dataStores.LoggerStore.ContainsKey(key))
                    {
                        this.internalLogger.LogError($"Internal error: Could not get logger with id: {key}. Skipping event.");
                        return false;
                    }

                    var logger = this.dataStores.LoggerStore.GetLogger(key);
                    var combinedAttributes = rule.Attributes.Combine(this.Otel.Attributes);

                    success = logger.ProcessLogMessage(message, rule, subscription.Variables, this.internalLogger, combinedAttributes);

                    sw.Stop();

                    var tags = new TagList();
                    tags.Add("processor.name", this.Name);
                    tags.Add("processor.id", this.Id);
                    tags.Add("processor.otel.connection", this.OtelConnection);
                    tags.Add("subscription.name", subscription.Name);
                    tags.Add("subscription.id", subscription.Id);
                    tags.Add("subscription.connection", subscription.BrokerConnection);
                    tags.Add("rule.name", rule.Name);
                    tags.Add("rule.id", rule.Id);
                    tags.Add("rule.connection", rule.OtelConnection);
                    tags.Add("rule.category", rule.CategoryName);
                    tags.Add("rule.payload_type", rule.PayloadType);
                    this.processorMeter.ProcessingTimeLoggingRule.Record(swTransform.ElapsedTicks + sw.ElapsedTicks, tags);
                    this.processorMeter.LogEntries.Add(1, tags);
                }
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
        /// <param name="message">The received message.</param>
        /// <param name="variables">The variables that can be applied to the payload.</param>
        /// <returns></returns>
        private void WriteValueToSignalStore(Guid subscriptionId, Guid ruleId, Otel otelSettings, OtelMetricRule rule, MqttMessage message, IEnumerable<Variable> variables)
        {
            if (rule.Name == null) return;

            var combinedAttributes = otelSettings.Attributes.Combine(rule.Attributes);

            if (rule.CreateAttributesFromUserProperties.HasValue && rule.CreateAttributesFromUserProperties == true)
            {
                combinedAttributes = combinedAttributes.Combine(message.UserProperties.ToOtelAttributes());
            }

            if (otelSettings.TopicAttributes != null)
            {
                combinedAttributes = combinedAttributes.Combine(TopicAttributeParser.Parse(message.Topic, otelSettings.TopicAttributes));
            }

            if (rule.TopicAttributes != null)
            {
                combinedAttributes = combinedAttributes.Combine(TopicAttributeParser.Parse(message.Topic, rule.TopicAttributes));
            }

            IEnumerable<OtelAttribute> expandedAttributes = EmbeddedExpressionParser.Expand(combinedAttributes, variables, message);

            try
            {
                switch (rule.SignalDataType)
                {
                    case SignalDataType.Float:
                        UpdateSignalStoreValue<float>(subscriptionId, ruleId, rule, message, expandedAttributes, variables);
                        break;
                    case SignalDataType.Int:
                        UpdateSignalStoreValue<int>(subscriptionId, ruleId, rule, message, expandedAttributes, variables);
                        break;
                    case SignalDataType.Double:
                        UpdateSignalStoreValue<double>(subscriptionId, ruleId, rule, message, expandedAttributes, variables);
                        break;
                    case SignalDataType.Long:
                        UpdateSignalStoreValue<long>(subscriptionId, ruleId, rule, message, expandedAttributes, variables);
                        break;
                    case SignalDataType.Decimal:
                        UpdateSignalStoreValue<decimal>(subscriptionId, ruleId, rule, message, expandedAttributes, variables);
                        break;
                    case SignalDataType.String:
                        UpdateSignalStoreValue<string>(subscriptionId, ruleId, rule, message, expandedAttributes, variables);
                        break;
                    case SignalDataType.DateTime:
                        UpdateSignalStoreValue<DateTime>(subscriptionId, ruleId, rule, message, expandedAttributes, variables);
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
        /// <param name="message">The message to be parsed.</param>
        /// <param name="expandedAttributes">The attributes to be applied to the value.</param>
        /// <param name="variables">The currently active variables.</param>
        /// <returns></returns>
        private void UpdateSignalStoreValue<T>(Guid subscriptionId, Guid ruleId, OtelMetricRule rule, MqttMessage message, IEnumerable<OtelAttribute> expandedAttributes, IEnumerable<Variable> variables)
        {
            T value = this.payloadParser.Parse<T>(rule.Name, rule.Value, new ParsingContext(variables, message));
            this.dataStores.SignalStore.UpdateValue(subscriptionId, ruleId, value, expandedAttributes);
        }

    }
}