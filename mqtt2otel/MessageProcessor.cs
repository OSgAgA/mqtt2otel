using Microsoft.Extensions.Logging;
using mqtt2otel.Helper;
using mqtt2otel.Interfaces;
using mqtt2otel.InternalLogging;
using mqtt2otel.InternalMetrics;
using mqtt2otel.Manifest;
using mqtt2otel.Parser;
using mqtt2otel.Shared.Helper;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace mqtt2otel
{
    public class MessageProcessor
    {
        /// <summary>
        /// The store for keeping trace and span informations.
        /// </summary>
        private ITraceStore traceStore;

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
        /// The message handling error manager.
        /// </summary>
        private MessageProcessingErrorManager errorManager;

        /// <summary>
        /// Creates a new instance of the <see cref="Processor"/> type.
        /// </summary>
        /// <param name="internalLogger">The logger used internaly for logging.</param>
        /// <param name="payloadParser">The payload parser for processing payloads.</param>
        /// <param name="payloadTransformation">The object used for processing payload transformations.</param>
        /// <param name="dataStores">The data stores used by the application to exchange data asynchronously.</param>
        /// <param name="meter">The meter for recording internal metrics.</param>
        /// <param name="usageTelemetryMeter">The meter for tracking usage telemetry.</param>
        /// <param name="embeddedExpressionParser">The parser used for parsing expressions embedded in a subscription.</param>
        /// <param name="traceStore">The trace store</param>
        public MessageProcessor(ILogger<MessageProcessor> internalLogger, IPayloadParser payloadParser, IPayloadTransformation payloadTransformation, IDataStores dataStores,
                         ProcessorMeter meter, UsageTelemetryMeter usageTelemetryMeter, IEmbeddedExpressionParser embeddedExpressionParser, ITraceStore traceStore)
        {
            this.processorMeter = meter;
            this.usageTelemetryMeter = usageTelemetryMeter;
            this.internalLogger = internalLogger;
            this.payloadParser = payloadParser;
            this.payloadTransformation = payloadTransformation;
            this.dataStores = dataStores;
            this.embeddedExpressionParser = embeddedExpressionParser;
            this.traceStore = traceStore;
            this.errorManager = new MessageProcessingErrorManager(this.internalLogger);
        }

        /// <summary>
        /// Process a subscription payload that was received from the mqtt broker.
        /// </summary>
        /// <param name="message">The received message.</param>
        /// <param name="subscription">The subscription that received the payload.</param>
        /// <param name="processor">The manifest processor.</param>
        /// <returns>A value indicating whether the operation has been successful.</returns>
        public bool ProcessMessage(MqttMessage message, MqttSubscription subscription, Processor processor)
        {
            int errorCount = 0;

            this.usageTelemetryMeter.MessageCount.Add(1);

            TagList tags = CreateDefaultTags(subscription, processor, null);

            var sw = new Stopwatch();
            sw.Start();
            try
            {
                using (this.internalLogger.StartActivity("Process metrics processors"))
                {
                    errorCount += this.ProcessMetricsSubscription(message, subscription, processor);
                }
                using (this.internalLogger.StartActivity("Process log processors"))
                {
                    errorCount += this.ProcessLogsSubscription(message, subscription, processor);
                }
                using (this.internalLogger.StartActivity("Process trace processors"))
                {
                    errorCount += this.ProcessTracesSubscriptions(message, subscription, processor);
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
        /// Process a message by applying tracing rules.
        /// </summary>
        /// <param name="message">The mqtt message received.</param>
        /// <param name="subscription">The settings of the subscription that triggered this processor.</param>
        /// <param name="processor">The manifest processor.</param>
        /// <returns>The number of rules, that run into an error.</returns>
        private int ProcessTracesSubscriptions(MqttMessage message, MqttSubscription subscription, Processor processor)
        {
            int errorCount = 0;

            foreach (var rule in processor.Otel.Traces)
            {
                lock (rule.BlockItemGate)
                {
                    if (!rule.IsActive || rule.BlockedTopics.Contains(message.Topic)) continue;
                }


                using (this.internalLogger.StartActivity("Process trace rule"))
                {
                    try
                    {
                        var sw = new Stopwatch();
                        sw.Start();
                        var combinedVariables = processor.Mqtt.Variables.Combine(subscription.Variables);

                        var parsingContext = new ParsingContext(combinedVariables, message);

                        var ruleClone = rule.Clone();
                        foreach (var action in ruleClone.Actions)
                        {
                            bool success = true;

                            foreach (var condition in action.When)
                            {
                                if (!string.IsNullOrWhiteSpace(condition))
                                {
                                    success &= this.payloadParser.Parse<bool>(condition, parsingContext);
                                }
                            }

                            if (success)
                            {
                                action.Then.Apply(ruleClone, this.embeddedExpressionParser, this.payloadParser, parsingContext);
                            }
                        }

                        foreach (var span in ruleClone.CreateSpans)
                        {
                            lock (span.BlockItemGate)
                            {
                                if (span.BlockedItems.Contains(span.Name) || (span.BlockedItemTopics.ContainsKey(span.Name) && span.BlockedItemTopics[span.Name].Contains(message.Topic))) continue;
                            }

                            try
                            {
                                var clone = span.Clone();
                                clone.Expand(this.embeddedExpressionParser, this.payloadParser, parsingContext);                                
                                if (string.IsNullOrEmpty(clone.Started)) clone.Started = this.GetTimestampFromMessageOrUtcNow(message).ToString("O");
                                if (clone.Kind == null) clone.Kind = ActivityKind.Internal;
                                if (clone.Status == null) clone.Status = ActivityStatusCode.Unset;
                                this.traceStore.StoreSpan(clone);
                            }
                            catch (Exception ex)
                            {
                                this.errorManager.HandleProcessingError("trace", span, span.Name, message, ex.Message, processor);
                                errorCount++;
                            }
                        }

                        foreach (var evt in ruleClone.CreateEvents)
                        {
                            lock (evt.BlockItemGate)
                            {
                                if (evt.BlockedItems.Contains(evt.Name) || (evt.BlockedItemTopics.ContainsKey(evt.Name) && evt.BlockedItemTopics[evt.Name].Contains(message.Topic))) continue;
                            }

                            try
                            {
                                var clone = evt.Clone();
                                if (string.IsNullOrEmpty(clone.Timestamp)) clone.Timestamp = this.GetTimestampFromMessageOrUtcNow(message).ToString("O");
                                clone.Expand(this.payloadParser, this.embeddedExpressionParser, parsingContext);

                                this.traceStore.AddEvent(clone);
                            }
                            catch (Exception ex)
                            {
                                this.errorManager.HandleProcessingError("trace", evt, evt.Name, message, ex.Message, processor);
                                errorCount++;
                            }
                        }

                        foreach (var span in ruleClone.EndSpans)
                        {
                            lock (span.BlockItemGate)
                            {
                                if (span.BlockedItems.Contains(span.Name) || (span.BlockedItemTopics.ContainsKey(span.Name) && span.BlockedItemTopics[span.Name].Contains(message.Topic))) continue;
                            }

                            try
                            {
                                var clone = span.Clone();
                                clone.Expand(this.embeddedExpressionParser, this.payloadParser, parsingContext);
                                if (string.IsNullOrEmpty(clone.Completed)) clone.Completed = this.GetTimestampFromMessageOrUtcNow(message).ToString("O");
                                this.traceStore.StoreSpan(clone, updateExisting: true);
                            }
                            catch (Exception ex)
                            {
                                this.errorManager.HandleProcessingError("trace", span, span.Name, message, ex.Message, processor);
                                errorCount++;
                            }
                        }

                        sw.Stop();

                        TagList tags = CreateDefaultTags(subscription, processor, rule);
                        this.processorMeter.ProcessingTimeTraceRule.Record(sw.ElapsedMicroseconds, tags);
                        this.usageTelemetryMeter.TraceProcessingTime.Record(sw.ElapsedMicroseconds);
                        this.processorMeter.Metrics.Add(1, tags);
                    }
                    catch (Exception ex)
                    {
                        this.errorManager.HandleProcessingError("trace", rule, null, message, ex.Message, processor);
                        errorCount++;
                    }
                }
            }

            return errorCount;
        }

        /// <summary>
        /// Process a subscription message by applying all metric rules.
        /// </summary>
        /// <param name="message">The received message.</param>
        /// <param name="subscription">The settings of the subscription that triggered this processor.</param>
        /// <param name="processor">The manifest processor.</param>
        /// <returns>The number of rules, that run into an error.</returns>
        private int ProcessMetricsSubscription(MqttMessage message, MqttSubscription subscription, Processor processor)
        {
            int errorCount = 0;

            foreach (var rule in processor.Otel.Metrics)
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
                        var combinedVariables = processor.Mqtt.Variables.Combine(subscription.Variables);
                        bool success = this.WriteValueToSignalStore(subscription, rule, processor.Otel, message, combinedVariables, processor);

                        if (!success)
                        {
                            errorCount++;
                        }

                        sw.Stop();

                        TagList tags = CreateDefaultTags(subscription, processor, rule);
                        tags.Add("rule.instrument", rule.Instrument);
                        this.processorMeter.ProcessingTimeMetricRule.Record(sw.ElapsedMicroseconds, tags);
                        this.usageTelemetryMeter.MetricProcessingTime.Record(sw.ElapsedMicroseconds);
                        this.processorMeter.Metrics.Add(1, tags);
                    }
                    catch (Exception ex)
                    {
                        this.errorManager.HandleProcessingError("metric", rule, null, message, ex.Message, processor);
                        errorCount++;
                    }
                }
            }

            return errorCount;
        }

        /// <summary>
        /// Creates the default tags that will be added to every internal metric signal.
        /// </summary>
        /// <param name="subscription">The subscription information.</param>
        /// <param name="processor">The processor that triggered the event.</param>
        /// <param name="rule">The rule.</param>
        /// <returns>The created default tags.</returns>
        private static TagList CreateDefaultTags(MqttSubscription subscription, Processor processor, Manifest.Rule? rule)
        {
            var tags = new TagList();
            tags.Add("processor.name", processor.Name);
            tags.Add("processor.otel.connection", processor.OtelConnection);
            tags.Add("subscription.name", subscription.Name);
            tags.Add("subscription.connection", subscription.BrokerConnection);
            if (rule != null)
            {
                tags.Add("rule.name", rule.Name);
                tags.Add("rule.connection", rule.OtelConnection);
            }

            return tags;
        }

        /// <summary>
        /// Process a subscription message that has been identified as a logging rule.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="subscription">The subscription that triggered the event.</param>
        /// <param name="processor">The manifest processor.</param>
        /// <returns>The number of rules, that run into an error.</returns>
        private int ProcessLogsSubscription(MqttMessage message, MqttSubscription subscription, Processor processor)
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
                        var combinedVariables = processor.Mqtt.Variables.Combine(subscription.Variables);
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

            foreach (var rule in processor.Otel.Logs)
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

                        var key = rule.InternalId;
                        if (!this.dataStores.LoggerStore.ContainsKey(key))
                        {
                            this.internalLogger.LogError("Internal error: Could not get logger with id: {RuleId}. Skipping event.", key);
                            errorCount++;
                            continue;
                        }

                        var logger = this.dataStores.LoggerStore.GetLogger(key);
                        var combinedAttributes = rule.Attributes.Combine(processor.Otel.Attributes);
                        if (rule.TopicAttributes != null)
                        {
                            combinedAttributes = combinedAttributes.Combine(TopicAttributeParser.Parse(message.Topic, rule.TopicAttributes));
                        }

                        if (rule.CreateAttributesFromUserProperties.HasValue && rule.CreateAttributesFromUserProperties == true)
                        {
                            combinedAttributes = combinedAttributes.Combine(message.UserProperties.ToOtelAttributes());
                        }

                        if (rule.SpanId != null)
                        {
                            var spanId = this.embeddedExpressionParser.Expand(rule.SpanId, new ParsingContext(subscription.Variables, message))?.ToString() ?? string.Empty;

                            if (!string.IsNullOrWhiteSpace(spanId))
                            {
                                var context = this.traceStore.GetOrCreateSpanContext(spanId);
                                combinedAttributes.AddInternalAttribute(InternalAttributes.TraceId, context.TraceId);
                                combinedAttributes.AddInternalAttribute(InternalAttributes.SpanId, context.SpanId);
                            }
                        }

                        logger.ProcessLogMessage(message, rule, subscription.Variables, this.internalLogger, combinedAttributes);

                        sw.Stop();

                        TagList tags = CreateDefaultTags(subscription, processor, rule);
                        tags.Add("rule.category", rule.CategoryName);
                        tags.Add("rule.payload_type", rule.PayloadType);
                        this.processorMeter.ProcessingTimeLoggingRule.Record(swTransform.ElapsedTicks + sw.ElapsedTicks, tags);
                        this.usageTelemetryMeter.LogProcessingTime.Record(swTransform.ElapsedTicks + sw.ElapsedTicks);
                        this.processorMeter.LogEntries.Add(1, tags);
                    }
                    catch (Exception ex)
                    {
                        this.errorManager.HandleProcessingError("log", rule, null, message, ex.Message, processor);
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
        /// <param name="processor">The manifest processor.</param>
        /// <returns>A value indicating whether all values have been successfully written to the signal store.</returns>
        private bool WriteValueToSignalStore(MqttSubscription subscription, OtelMetricRule rule, Otel otelSettings, MqttMessage message, IEnumerable<Variable> variables, Processor processor)
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

            IEnumerable<OtelAttribute> expandedAttributes = this.embeddedExpressionParser.Expand(combinedAttributes, new ParsingContext(variables, message));


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
                        this.errorManager.HandleProcessingError("metric", rule, item.Key, message, ex.Message, processor);
                        success = false;
                    }
                }
            }
            catch (Exception ex)
            {
                this.errorManager.HandleProcessingError("metric", rule, null, message, ex.Message, processor);
                success = false;
            }

            return success;
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

        /// <summary>
        /// A list of identifiers used to identify a timestamp inside a message.
        /// </summary>
        private List<string> timestampIdentifiers = new List<string>() { "timestamp", "ts", "time" };

        /// <summary>
        /// Get the timestamp from a message or utc now if no timestamp was found.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <returns>The timestamp from the message or utc now.</returns>
        private DateTime GetTimestampFromMessageOrUtcNow(MqttMessage message)
        {
            DateTime result = DateTime.UtcNow;

            foreach (var prop in message.UserProperties)
            {
                if (this.timestampIdentifiers.Any(s => s.Contains(prop.Name.ToLower())))
                {
                    DateTimeHelper.TryParseToUtc(prop.Value, out result);
                }
            }

            return result;
        }
    }
}
