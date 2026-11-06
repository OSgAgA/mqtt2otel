using Microsoft.Extensions.Logging;
using mqtt2otel.Helper;
using mqtt2otel.Manifest;
using System;
using System.Collections.Generic;
using System.Text;

namespace mqtt2otel
{
    /// <summary>
    /// Provides methods for handling errors while processing mqtt messages.
    /// </summary>
    public class MessageProcessingErrorManager
    {
        /// <summary>
        /// The internal logger of the application.
        /// </summary>
        private ILogger internalLogger;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageProcessingErrorManager"/> class.
        /// </summary>
        /// <param name="logger">The internal logger of the application.</param>
        public MessageProcessingErrorManager(ILogger logger)
        {
            this.internalLogger = logger; 
        }

        /// <summary>
        /// Handles a error, that appeared while processing a mqtt message inside this processor.
        /// </summary>
        /// <param name="signalType">The siganl type namme (metric, log, or trace).</param>
        /// <param name="rule">The rule that caused the error.</param>
        /// <param name="itemName">The name of the item inside the rule, that caused the message (e.g. a json property). May be null if no item is involved.</param>
        /// <param name="mqttMessage">The mqtt message.</param>
        /// <param name="errorMessage">The error message.</param>
        /// <param name="processor">The manifest processor.</param>
        public void HandleProcessingError(string signalType, Rule rule, string? itemName, MqttMessage mqttMessage, string errorMessage, Processor processor)
        {
            if (rule.LogMessageDetails ?? true)
            {
                this.internalLogger.LogError("Rule: '{Rule}', Processor: '{Processor}', Item: {Item}', Message: {Message}, Mqtt message details: [{MqttMessage}]", rule.Name, processor.Name, itemName ?? "<<none>>", errorMessage, mqttMessage.ToString().SanitizeForLog());
            }
            else
            {
                this.internalLogger.LogError("Rule: '{RuleName}', Processor: '{ProcessorName}', Item: '{ItemName}', Message: {Message}", rule.Name, processor.Name, itemName ?? "<<none>>", errorMessage);
            }

            if (rule.AllowRuleDeactivationByTopic ?? true)
            {
                if (itemName == null)
                {
                    rule.BlockedTopics.Add(mqttMessage.Topic);
                    this.internalLogger.LogError("Topic '{Topic}' has been blocked for " + signalType + " rule '{Rule}' inside processor '{Processor}'. Current blocked topic count is: {BlockedTopicCount}/{BlockedTopicLimit}.",
                        mqttMessage.Topic.SanitizeForLog(), rule.Name, processor.Name, rule.BlockedTopics.Count, rule.BlockedTopicLimit);
                }
                else
                {
                    if (!rule.BlockedItemTopics.ContainsKey(itemName)) rule.BlockedItemTopics[itemName] = new();

                    rule.BlockedItemTopics[itemName].Add(mqttMessage.Topic);
                    this.internalLogger.LogError("Topic '{Topic}' has been blocked for Item '{Item}' at " + signalType + " rule '{Rule}' inside processor '{Processor}'. Current blocked topic count is: {BlockedTopicCount}/{BlockedTopicLimit}.",
                        mqttMessage.Topic.SanitizeForLog(), itemName, rule.Name, processor.Name, rule.BlockedItemTopics[itemName].Count, rule.BlockedTopicLimit);
                }
            }

            lock (rule.BlockItemGate)
            {
                if (itemName == null)
                {
                    if (rule.BlockedTopics.Count > rule.BlockedTopicLimit && (rule.AllowRuleDeactivation ?? true))
                    {
                        rule.IsActive = false;
                        this.internalLogger.LogError(signalType + " rule '{Rule}' inside processor '{Processor}' has exceeded the maximum blocked topic limit ({BlockedTopicLimit}) and is therefor disabled.", rule.Name, processor.Name, rule.BlockedTopicLimit);
                    }
                }
                else
                {

                    if (rule.BlockedItemTopics[itemName].Count > rule.BlockedTopicLimit)
                    {
                        if (rule.AllowRuleDeactivationByItem ?? true)
                        {
                            rule.BlockedItems.Add(itemName);
                            this.internalLogger.LogError("Item '{Item}' on " + signalType + " rule '{Rule}' inside processor '{Processor}' has exceeded the maximum blocked topic limit ({BlockedTopicLimit}) and is therefor disabled.", itemName, rule.Name, processor.Name, rule.BlockedTopicLimit);
                        }
                    }
                }
            }
        }

    }
}
