using mqtt2otel.Helper;
using mqtt2otel.Interfaces;
using mqtt2otel.Manifest;
using mqtt2otel.Parser;
using System;
using System.Collections.Generic;
using System.Text;
using YamlDotNet.Core.Tokens;

namespace mqtt2otel.Stores
{
    /// <summary>
    /// Stores metric signals to be delivered to an open telemetry endpoint.
    /// </summary>
    public class SignalStore : ISignalStore
    {
        /// <summary>
        /// The metric values that are stored inside the signal store.
        /// </summary>
        private Dictionary<string, object> ValueStore = new();

        /// <summary>
        /// These callbacks will be executed when a value with the key of the dictionary is stored.
        /// </summary>
        private Dictionary<string, Action> Callbacks = new();

        /// <summary>
        /// The parser used for parsing expressions embedded in a subscription.
        /// </summary>
        private IEmbeddedExpressionParser embeddedExpressionParser;

        /// <summary>
        /// Initializes a new instance of the <see cref="SignalStore"/> class.
        /// </summary>
        /// <param name="embeddedExpressionParser">The parser used for parsing expressions embedded in a subscription.</param>
        public SignalStore(IEmbeddedExpressionParser embeddedExpressionParser)
        {
            this.embeddedExpressionParser = embeddedExpressionParser;
        }

        /// <summary>
        /// This action is called when no signal is found and a new signal for the given parameters must be created.
        /// </summary>
        public Action<MqttSubscription, OtelMetricRule, ParsingContext>? SignalCreator { get; set; } = null;

        /// <summary>
        /// Register a callback function that will be called when a value with the given key is stored or updaten in the signal store.
        /// </summary>
        /// <param name="subscriptionId">The id of the subscription that generated the message from which the signal is received.</param>
        /// <param name="ruleId">The id of the rule, that generated the message from which the signal is received.</param>
        /// <param name="signalName">The name of the signal that will be created (if not allready existing).</param>
        /// <param name="callback">The callback to be called.</param>
        public void RegisterCallback(Guid subscriptionId, Guid ruleId, string signalName, Action callback)
        {
            var key = this.GenerateKey(subscriptionId, ruleId, signalName);
            this.Callbacks[key] = callback;
        }

        /// <summary>
        /// Stores a value inside the signal store.
        /// 
        /// If registered a callback function will be called.
        /// </summary>
        /// <typeparam name="TPayload">The type of the payload that should be stored.</typeparam>
        /// <param name="subscription">The subscription that generated the message from which the signal is received.</param>
        /// <param name="rule">The rule, that generated the message from which the signal is received.</param>
        /// <param name="name">The name of the metric that will be stored.</param>
        /// <param name="payload">The payload that should be stored in the signal store.</param>
        public void StoreValue<TPayload>(MqttSubscription subscription, OtelMetricRule rule, string name, OtelMetric<TPayload> payload)
        {
            var key = this.GenerateKey(subscription, rule, name);
            this.ValueStore[key] = payload;

            if (this.Callbacks.ContainsKey(key)) this.Callbacks[key]();
        }

        /// <summary>
        /// Retrieves a value from the signal store.
        /// </summary>
        /// <typeparam name="TPayload">The type of the value.</typeparam>
        /// <param name="subscriptionId">The id of the subscription that generated the message from which the signal is received.</param>
        /// <param name="ruleId">The id of the rule, that generated the message from which the signal is received.</param>
        /// <param name="signalName">The name of the signal that will be created (if not allready existing).</param>
        /// <returns>The value as the given type.</returns>
        /// <exception cref="Mqtt2OtelException">Thrown if the value cannot be cast to the given type.</exception>
        public OtelMetric<TPayload> GetValue<TPayload>(Guid subscriptionId, Guid ruleId, string signalName)
        {
            var key = this.GenerateKey(subscriptionId, ruleId, signalName);

            if (!(this.ValueStore[key] is OtelMetric<TPayload>))
                throw new Mqtt2OtelException($"Cannot get value from {nameof(SignalStore)}. Key ({key}) returned an object of type {this.ValueStore[key].GetType().FullName}, but type {typeof(OtelMetric<TPayload>).FullName} was expected.");

            return (OtelMetric<TPayload>)this.ValueStore[key];
        }

        /// <summary>
        /// Tests if the store contains the given key.
        /// </summary>
        /// <param name="subscriptionId">The id of the subscription that generated the message from which the signal is received.</param>
        /// <param name="ruleId">The id of the rule, that generated the message from which the signal is received.</param>
        /// <param name="signalName">The name of the signal that will be created (if not allready existing).</param>
        /// <returns>A value indicating whether the key exists inside the signal store.</returns>
        public bool ContainsKey(Guid subscriptionId, Guid ruleId, string signalName)
        {
            var key = this.GenerateKey(subscriptionId, ruleId, signalName);

            return this.ValueStore.ContainsKey(key);
        }

        /// <summary>
        /// Updates a value. The key for the value must already exist.
        /// 
        /// Calls a callback function if registered.
        /// </summary>
        /// <typeparam name="TPayload">The type of the value to be updated.</typeparam>
        /// <param name="subscription">The subscription that generated the message from which the signal is received.</param>
        /// <param name="rule">The rule, that generated the message from which the signal is received.</param>
        /// <param name="context">The current parsing context.</param>
        /// <param name="value">The new value.</param>
        /// <param name="attributes">Attributes that should be added to the metric value.</param>
        public void UpdateValue<TPayload>(MqttSubscription subscription, OtelMetricRule rule, ParsingContext context, TPayload value, IEnumerable<OtelAttribute> attributes)
        {
            string signalName = this.embeddedExpressionParser.Expand(rule.Name, context);
            var key = this.GenerateKey(subscription.Id, rule.Id, signalName);

            if (this.SignalCreator != null && !this.ContainsKey(subscription.Id, rule.Id, signalName)) this.SignalCreator(subscription, rule, context);
            var metric = this.GetValue<TPayload>(subscription.Id, rule.Id, signalName);

            metric.Value = value;
            metric.Attributes = attributes;

            if (this.Callbacks.ContainsKey(key)) this.Callbacks[key]();
        }

        /// <summary>
        /// Deletes all entries from the store.
        /// </summary>
        public void DeleteStore()
        {
            this.ValueStore.Clear();
            this.Callbacks.Clear();
        }

        /// <summary>
        /// Generates a key for storing the data in the <see cref="ISignalStore"/>.
        /// </summary>
        /// <param name="subscription">The subscription that generated the message from which the signal is received.</param>
        /// <param name="rule">The rule, that generated the message from which the signal is received.</param>
        /// <param name="name">The signal name.</param>
        /// <returns>The generated key.</returns>
        private string GenerateKey(MqttSubscription subscription, OtelMetricRule rule, string name)
        {
            return this.GenerateKey(subscription.Id, rule.Id, name);
        }

        /// <summary>
        /// Generates a key for storing the data in the <see cref="ISignalStore"/>.
        /// </summary>
        /// <param name="subscriptionId">The id of the subscription that generated the message from which the signal is received.</param>
        /// <param name="ruleId">The id of the rule, that generated the message from which the signal is received.</param>
        /// <param name="name">The signal name.</param>
        /// <returns>The generated key.</returns>
        private string GenerateKey(Guid subscriptionId, Guid ruleId, string name)
        {
            return $"{subscriptionId}:{ruleId}:{name}";
        }
    }
}
