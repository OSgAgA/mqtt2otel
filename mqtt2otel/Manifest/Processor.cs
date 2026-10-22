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
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml.Linq;
using YamlDotNet.Core.Tokens;

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
        /// The meter for recording usage telemetry.
        /// </summary>
        private UsageTelemetryMeter usageTelemetryMeter;

        /// <summary>
        /// The parser used for parsing expressions embedded in a subscription.
        /// </summary>
        private IEmbeddedExpressionParser embeddedExpressionParser;

        /// <summary>
        /// Creates a new instance of the <see cref="Processor"/> type.
        /// </summary>
        /// <param name="internalLogger">The logger used internaly for logging.</param>
        /// <param name="payloadParser">The payload parser for processing payloads.</param>
        /// <param name="payloadTransformation">The object used for processing payload transformations.</param>
        /// <param name="dataStores">The data stores used by the application to exchange data asynchronously.</param>
        /// <param name="meter">The meter for recording internal metrics.</param>
        /// <param name="embeddedExpressionParser">The parser used for parsing expressions embedded in a subscription.</param>
        public Processor(ILogger internalLogger, IPayloadParser payloadParser, IPayloadTransformation payloadTransformation, IDataStores dataStores,
                         ProcessorMeter meter, UsageTelemetryMeter usageTelemetryMeter, IEmbeddedExpressionParser embeddedExpressionParser)
        {
            this.processorMeter = meter;
            this.usageTelemetryMeter = usageTelemetryMeter;
            this.internalLogger = internalLogger;
            this.payloadParser = payloadParser;
            this.payloadTransformation = payloadTransformation;
            this.dataStores = dataStores;
            this.embeddedExpressionParser = embeddedExpressionParser;
        }

        /// <summary>
        /// Gets or sets a value indicating, whether attributes should be created from mqtt user properties (true), or not (false), or
        /// if the default setting should be used (null).
        /// </summary>
        [InheritedProperty]
        public bool? CreateAttributesFromUserProperties { get; set; } = null;

        /// <summary>
        /// Gets or sets a value indicating whether the mqtt message details should be logged in case of an error. Set to null to use the default value.
        /// </summary>
        [InheritedProperty]
        public bool? LogMessageDetails { get; set; } = null;

        /// <summary>
        /// Gets or sets a value indicating whether the rule can be deactivated, when the error rate exceeds <see cref="MaxErrorRateInPercentage"/>.
        /// Set to null to use default value.
        /// </summary>
        [InheritedProperty]
        public bool? AllowRuleDeactivation { get; set; } = null;

        /// <summary>
        /// Gets or sets a value indicating whether the rule can deactivate topics from executing this rule, when the error rate for the topic is to high.
        /// Set to null to use default value.
        /// </summary>
        [InheritedProperty]
        public bool? AllowRuleDeactivationByTopic { get; set; } = null;

        /// <summary>
        /// Gets or sets a value indicating whether the rule can deactivate topics from executing this rule on a scpecific item (e.g. a property of a json message), when the error rate for the topic is to high.
        /// Set to null to use default value.
        /// </summary>
        [InheritedProperty]
        public bool? AllowRuleDeactivationByItem { get; set; } = null;

        /// <summary>
        /// Gets or sets the maximum number of topics that are blocked for an item / rule. If this threshold is exceeded the whole item or rule is blocked.
        /// </summary>
        [InheritedProperty]
        public int? BlockedTopicLimit { get; set; } = null;

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
        /// Gets or sets the open telemetry scope that should be used for the metric.
        /// Set to null to use the default scope of the <see cref="OtelConnection"/>.
        /// </summary>
        [InheritedProperty]
        public string? OtelScope { get; set; } = null;

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
            int errorCount = 0;

            this.usageTelemetryMeter.MessageCount.Add(1);

            var tags = new TagList();
            tags.Add("subscription.name", subscription.Name);
            tags.Add("subscription.connection", subscription.BrokerConnection);
            tags.Add("processor.name", this.Name);
            tags.Add("processor.otel.connection", this.OtelConnection);

            var sw = new Stopwatch();
            sw.Start();
            try
            {
                using (this.internalLogger.StartActivity("Process metrics processors"))
                {
                    errorCount += this.ProcessMetricsSubscription(message, subscription);
                }
                using (this.internalLogger.StartActivity("Process log processors"))
                {
                    errorCount += this.ProcessLogsSubscription(message, subscription);
                }
            }
            catch
            {
                sw.Stop();
                this.processorMeter.ProcessingErrorCount.Add(1, tags);
                return false;
            }

            sw.Stop();

            this.processorMeter.ProcessingTimeTotal.Record(sw.ElapsedMicroseconds, tags);

            if (errorCount > 0) this.processorMeter.ProcessingErrorCount.Add(errorCount, tags);

            return errorCount == 0;
        }

        /// <summary>
        /// Process a subscription message by applying all metric rules..
        /// </summary>
        /// <param name="message">The received message.</param>
        /// <param name="subscription">The settings of the subscription that triggered this processor.</param>
        /// <returns>The number of rules, that run into an error.</returns>
        private int ProcessMetricsSubscription(MqttMessage message, MqttSubscription subscription)
        {
            int errorCount = 0;

            foreach (var rule in this.Otel.Metrics)
            {
                lock (rule.BlockItemGate)
                {
                    if (!rule.IsActive || rule.BlockedTopics.Contains(message.Topic)) continue;
                }

                if (rule.Name == null) continue;

                using (this.internalLogger.StartActivity("Process metrics rule"))
                {
                    try
                    {
                        var sw = new Stopwatch();
                        sw.Start();
                        var combinedVariables = this.Mqtt.Variables.Combine(subscription.Variables);
                        bool success = this.WriteValueToSignalStore(subscription, rule, this.Otel, message, combinedVariables);

                        if (!success)
                        {
                            errorCount++;
                        }

                        sw.Stop();

                        var tags = new TagList();
                        tags.Add("processor.name", this.Name);
                        tags.Add("processor.otel.connection", this.OtelConnection);
                        tags.Add("subscription.name", subscription.Name);
                        tags.Add("subscription.connection", subscription.BrokerConnection);
                        tags.Add("rule.name", rule.Name);
                        tags.Add("rule.connection", rule.OtelConnection);
                        tags.Add("rule.instrument", rule.Instrument);
                        this.processorMeter.ProcessingTimeMetricRule.Record(sw.ElapsedMicroseconds, tags);
                        this.usageTelemetryMeter.MetricProcessingTime.Record(sw.ElapsedMicroseconds);
                        this.processorMeter.Metrics.Add(1, tags);
                    }
                    catch (Exception ex)
                    {
                        this.HandleProcessingError("metric", rule, null, message, ex.Message);
                        errorCount++;
                    }
                }
            }

            return errorCount;
        }

        /// <summary>
        /// Process a subscription message that has been identified as a logging rule.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="subscription">The subscription that triggered the event.</param>
        /// <returns>The number of rules, that run into an error.</returns>
        private int ProcessLogsSubscription(MqttMessage message, MqttSubscription subscription)
        {
            int errorCount = 0;
            var swTransform = new Stopwatch();

            Exception? transformException = null;
            using (this.internalLogger.StartActivity("Transform payload"))
            {
                try
                {
                    swTransform.Start();
                    if (!string.IsNullOrWhiteSpace(subscription.Transform))
                    {
                        var combinedVariables = this.Mqtt.Variables.Combine(subscription.Variables);
                        message.Payload = this.payloadTransformation.Apply(subscription.Transform, new ParsingContext(combinedVariables, message));
                    }
                    swTransform.Stop();
                }
                catch (Exception ex)
                {
                    swTransform.Stop();
                    transformException = ex;
                }
            }

            foreach (var rule in this.Otel.Logs)
            {
                lock (rule.BlockItemGate)
                {
                    if (!rule.IsActive || rule.BlockedTopics.Contains(message.Topic)) continue;
                }

                using (this.internalLogger.StartActivity("Process log rule"))
                {

                    try
                    {
                        if (transformException != null) throw transformException;

                        var sw = new Stopwatch();
                        sw.Start();

                        var key = rule.Id;
                        if (!this.dataStores.LoggerStore.ContainsKey(key))
                        {
                            this.internalLogger.LogError("Internal error: Could not get logger with id: {RuleId}. Skipping event.", key);
                            errorCount++;
                            continue;
                        }

                        var logger = this.dataStores.LoggerStore.GetLogger(key);
                        var combinedAttributes = rule.Attributes.Combine(this.Otel.Attributes);

                        if (rule.CreateAttributesFromUserProperties.HasValue && rule.CreateAttributesFromUserProperties == true)
                        {
                            combinedAttributes = combinedAttributes.Combine(message.UserProperties.ToOtelAttributes());
                        }

                        logger.ProcessLogMessage(message, rule, subscription.Variables, this.internalLogger, combinedAttributes);

                        sw.Stop();

                        var tags = new TagList();
                        tags.Add("processor.name", this.Name);
                        tags.Add("processor.otel.connection", this.OtelConnection);
                        tags.Add("subscription.name", subscription.Name);
                        tags.Add("subscription.connection", subscription.BrokerConnection);
                        tags.Add("rule.name", rule.Name);
                        tags.Add("rule.connection", rule.OtelConnection);
                        tags.Add("rule.category", rule.CategoryName);
                        tags.Add("rule.payload_type", rule.PayloadType);
                        this.processorMeter.ProcessingTimeLoggingRule.Record(swTransform.ElapsedTicks + sw.ElapsedTicks, tags);
                        this.usageTelemetryMeter.LogProcessingTime.Record(swTransform.ElapsedTicks + sw.ElapsedTicks);
                        this.processorMeter.LogEntries.Add(1, tags);
                    }
                    catch (Exception ex)
                    {
                        this.HandleProcessingError("log", rule, null, message, ex.Message);
                        errorCount++;
                    }
                }
            }

            return errorCount;
        }

        /// <summary>
        /// Stores a metric signal in the signal store.
        /// </summary>
        /// <param name="subscription">The subscription that generated the message from which the signal is received.</param>
        /// <param name="rule">The otel metric rule settings that should be used to process this signal.</param>
        /// <param name="otelSettings">The otel settings that should be used to process this signal.</param>
        /// <param name="message">The received message.</param>
        /// <param name="variables">The variables that can be applied to the payload.</param>
        /// <returns>A value indicating whether all values have been successfully written to the signal store.</returns>
        private bool WriteValueToSignalStore(MqttSubscription subscription, OtelMetricRule rule, Otel otelSettings, MqttMessage message, IEnumerable<Variable> variables)
        {
            if (rule.Name == null) return false;

            bool success = true;

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

            IEnumerable<OtelAttribute> expandedAttributes = this.embeddedExpressionParser.Expand(combinedAttributes, variables, message);


            try
            {
                var context = new ParsingContext(variables, message);

                var measurement = OtelMeasurement.FromRule(rule, this.payloadParser, this.embeddedExpressionParser, context);
                measurement.Attributes = expandedAttributes;

                Dictionary<string, object?> valueData = new();

                switch (rule.ParseAs.Type)
                {
                    case ParseAsOptions.Undefined:
                        valueData[measurement.SignalName] = null;
                        break;
                    case ParseAsOptions.Json:
                        valueData = JsonFlattener.Flatten(message.Payload, rule.ParseAs.Separator, rule.ParseAs.NameOnly);
                        break;
                    case ParseAsOptions.Xml:
                        valueData = XmlFlattener.Flatten(message.Payload, rule.ParseAs.Separator, rule.ParseAs.NameOnly);
                        break;
                    default:
                        break;
                }

                foreach (var item in valueData)
                {
                    lock (rule.BlockItemGate)
                    {
                        if (rule.BlockedItems.Contains(item.Key) || (rule.BlockedItemTopics.ContainsKey(item.Key) && rule.BlockedItemTopics[item.Key].Contains(message.Topic))) continue;
                    }

                    try
                    {
                        var newMeasurement = measurement.Clone();
                        newMeasurement.SignalName = item.Key;
                        newMeasurement.Value = item.Value;
                        CallUpdateSignalStoreValueWithType(subscription, rule, newMeasurement, context);
                    }
                    catch (Exception ex)
                    {
                        this.HandleProcessingError("metric", rule, item.Key, message, ex.Message);
                        success = false;
                    }
                }
            }
            catch (Exception ex)
            {
                this.HandleProcessingError("metric", rule, null, message, ex.Message);
                success = false;
            }

            return success;
        }

        /// <summary>
        /// Handles a error, that appeared while processing a mqtt message inside this processor.
        /// </summary>
        /// <param name="type">The type namme (metric, log, or trace).</param>
        /// <param name="rule">The rule that caused the error.</param>
        /// <param name="itemName">The name of the item inside the rule, that caused the message (e.g. a json property). May be null if no item is involved.</param>
        /// <param name="mqttMessage">The mqtt message.</param>
        /// <param name="errorMessage">The error message.</param>
        private void HandleProcessingError(string type, Rule rule, string? itemName, MqttMessage mqttMessage, string errorMessage)
        {
            if (rule.LogMessageDetails ?? true)
            {
                this.internalLogger.LogError("Rule: '{Rule}', Processor: '{Processor}', Item: {Item}', Message: {Message}, Mqtt message details: [{MqttMessage}]", rule.Name, this.Name, itemName ?? "<<none>>", errorMessage, mqttMessage.ToString().SanitizeForLog());
            }
            else
            {
                this.internalLogger.LogError("Rule: '{RuleName}', Processor: '{ProcessorName}', Item: '{ItemName}', Message: {Message}", rule.Name, this.Name, itemName ?? "<<none>>", errorMessage);
            }

            if (rule.AllowRuleDeactivationByTopic ?? true)
            {
                if (itemName == null)
                {
                    rule.BlockedTopics.Add(mqttMessage.Topic);
                    this.internalLogger.LogError("Topic '{Topic}' has been blocked for " + type + " rule '{Rule}' inside processor '{Processor}'. Current blocked topic count is: {BlockedTopicCount}/{BlockedTopicLimit}.",
                        mqttMessage.Topic.SanitizeForLog(), rule.Name, this.Name, rule.BlockedTopics.Count, rule.BlockedTopicLimit);
                }
                else
                {
                    if (!rule.BlockedItemTopics.ContainsKey(itemName)) rule.BlockedItemTopics[itemName] = new();

                    rule.BlockedItemTopics[itemName].Add(mqttMessage.Topic);
                    this.internalLogger.LogError("Topic '{Topic}' has been blocked for Item '{Item}' at " + type + " rule '{Rule}' inside processor '{Processor}'. Current blocked topic count is: {BlockedTopicCount}/{BlockedTopicLimit}.",
                        mqttMessage.Topic.SanitizeForLog(), itemName, rule.Name, this.Name, rule.BlockedItemTopics[itemName].Count, rule.BlockedTopicLimit);
                }
            }

            lock (rule.BlockItemGate)
            {
                if (itemName == null)
                {
                    if (rule.BlockedTopics.Count > rule.BlockedTopicLimit && (rule.AllowRuleDeactivation ?? true))
                    {
                        rule.IsActive = false;
                        this.internalLogger.LogError(type + " rule '{Rule}' inside processor '{Processor}' has exceeded the maximum blocked topic limit ({BlockedTopicLimit}) and is therefor disabled.", rule.Name, this.Name, rule.BlockedTopicLimit);
                    }
                }
                else
                {
                    
                    if (rule.BlockedItemTopics[itemName].Count > rule.BlockedTopicLimit)
                    {
                        if (rule.AllowRuleDeactivationByItem ?? true)
                        {
                            rule.BlockedItems.Add(itemName);
                            this.internalLogger.LogError("Item '{Item}' on " + type + " rule '{Rule}' inside processor '{Processor}' has exceeded the maximum blocked topic limit ({BlockedTopicLimit}) and is therefor disabled.", itemName, rule.Name, this.Name, rule.BlockedTopicLimit);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Calls <see cref="UpdateSignalStoreValue{T}(MqttSubscription, OtelMetricRule, string, SignalDataType, ParsingContext, IEnumerable{OtelAttribute})"/> with the correct type.
        /// </summary>
        /// <typeparam name="T">The type of the value inside the store.</typeparam>
        /// <param name="subscription">The subscription that generated the message from which the signal is received.</param>
        /// <param name="rule">The otel metric rule that should be applied.</param>
        /// <param name="measurement">The measuremet to be send to the signal store..</param>
        /// <param name="context">The current parsing context..</param>
        private void CallUpdateSignalStoreValueWithType(MqttSubscription subscription, OtelMetricRule rule, OtelMeasurement measurement, ParsingContext context)
        {
            switch (rule.SignalDataType)
            {
                case SignalDataType.Default:
                    measurement.Value = measurement.Value != null ? measurement.Value : this.payloadParser.Parse(rule.Value, context);
                    break;
                case SignalDataType.Float:
                    measurement.Value = measurement.Value != null ? Convert.ToSingle(measurement.Value) : this.payloadParser.Parse<float>(rule.Value, context);
                    break;
                case SignalDataType.Int:
                    measurement.Value = measurement.Value != null ? TypeHelper.ConvertObject<int>(measurement.Value) : this.payloadParser.Parse<int>(rule.Value, context);
                    break;
                case SignalDataType.Bool:
                    bool boolValue = measurement.Value != null ? TypeHelper.ConvertObject<bool>(measurement.Value) : this.payloadParser.Parse<bool>(rule.Value, context);
                    measurement.Value = boolValue ? 1 : 0;
                    measurement.SignalDataType = SignalDataType.Int;
                    break;
                case SignalDataType.Double:
                    measurement.Value = measurement.Value != null ? Convert.ToDouble(measurement.Value) : this.payloadParser.Parse<double>(rule.Value, context);
                    break;
                case SignalDataType.Long:
                    measurement.Value = measurement.Value != null ? Convert.ToInt64(measurement.Value) : this.payloadParser.Parse<long>(rule.Value, context);
                    break;
                case SignalDataType.Decimal:
                    measurement.Value = measurement.Value != null ? Convert.ToDecimal(measurement.Value) : this.payloadParser.Parse<decimal>(rule.Value, context);
                    break;
                case SignalDataType.String:
                    measurement.Value = measurement.Value != null ? (measurement.Value.ToString() ?? string.Empty) : this.payloadParser.Parse<string>(rule.Value, context);
                    break;
                case SignalDataType.DateTime:
                    measurement.Value = measurement.Value != null ? (DateTime)measurement.Value : this.payloadParser.Parse<DateTime>(rule.Value, context);
                    break;
                default:
                    throw new ExpressionParsingException(new Exception(), $"Signal type {rule.SignalDataType} not supported.");
            }

            this.UpdateSignalStoreValue(subscription, rule, measurement, context);

        }

        /// <summary>
        /// Updates a value in the signal store.
        /// </summary>
        /// <typeparam name="T">The type of the value inside the store.</typeparam>
        /// <param name="subscription">The subscription that generated the message from which the signal is received.</param>
        /// <param name="rule">The otel metric rule that should be applied.</param>
        /// <param name="measurement">The measuremet to be send to the signal store..</param>
        /// <param name="context">The current parsing context..</param>
        private void UpdateSignalStoreValue(MqttSubscription subscription, OtelMetricRule rule, OtelMeasurement measurement, ParsingContext context)
        {
            // First: Apply actions.
            foreach (var action in rule.Actions)
            {
                var actionContext = context.Clone();
                actionContext.InternalVariables["Name"] = measurement.SignalName;
                actionContext.InternalVariables["Value"] = measurement.Value;
                actionContext.InternalVariables["Type"] = measurement.SignalDataType.ToString();

                var checkResult = true;

                foreach (var condition in action.When)
                {
                    if (!string.IsNullOrWhiteSpace(condition))
                    {
                        checkResult &= this.payloadParser.Parse<bool>(condition, actionContext);
                    }
                }

                if (checkResult)
                {
                    measurement = action.Then.Apply(measurement, this.embeddedExpressionParser, context);

                    if (action.Then.Output != null)
                    {
                        using (this.internalLogger.BeginScope(action.Then.Output.Attributes))
                        {
                            var message = this.embeddedExpressionParser.Expand(action.Then.Output.Message, context);
                            this.internalLogger.Log(action.Then.Output.Level, message.SanitizeForLog());
                        }
                    }

                    if (action.Then.Ignore) return;
                }
            }

            // Then convert the value and format the name.
            if (measurement.ValueConverter != null)
            {
                var valueConverterContext = context.Clone();
                valueConverterContext.InternalVariables["Value"] = measurement.Value;
                measurement.Value = this.payloadParser.Parse(measurement.ValueConverter, valueConverterContext);
            }

            if (measurement.NameFormatter != null)
            {
                var nameContext = context.Clone();
                nameContext.InternalVariables["Name"] = measurement.SignalName;
                measurement.SignalName = this.payloadParser.Parse<string>(measurement.NameFormatter, nameContext);
            }

            //Then write the data.
            if (measurement.Value != null)
            {
                measurement.SignalDataType = measurement.SignalDataType == SignalDataType.Default ? TypeHelper.ConvertTypeToSignalDataType(measurement.Value.GetType()) : measurement.SignalDataType;

                if (measurement.SignalDataType != SignalDataType.String && measurement.SignalDataType != SignalDataType.DateTime && measurement.SignalDataType != SignalDataType.Default)
                {
                    if (measurement.SignalDataType == SignalDataType.Bool && measurement.Value != null)
                    {
                        measurement.SignalDataType = SignalDataType.Int;
                        measurement.Value = (bool)measurement.Value ? 1 : 0;
                    }

                    TypeHelper.CallMethodWithGenericType(
                        this.dataStores.SignalStore,
                        measurement.SignalDataType,
                        "UpdateValue",
                        new object[] { measurement, context });
                }
            }
        }

    }
}