using mqtt2otel.Helper;
using mqtt2otel.Interfaces;
using mqtt2otel.Manifest;
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
        private Dictionary<string, Action<string>> Callbacks = new();

        /// <summary>
        /// Register a callback function that will be called when a value with the given key is stored or updaten in the signal store.
        /// </summary>
        /// <param name="subscriptionId">The id of the subscription that generated the message from which the signal is received.</param>
        /// <param name="ruleId">The id of the rule, that generated the message from which the signal is received.</param>
        /// <param name="callback">The callback to be called. Will get the key as the argument.</param>
        public void RegisterCallback(Guid subscriptionId, Guid ruleId, Action<string> callback)
        {
            var key = this.GenerateKey(subscriptionId, ruleId);
            this.Callbacks[key] = callback;
        }

        /// <summary>
        /// Stores a value inside the signal store.
        /// 
        /// If registered a callback function will be called.
        /// </summary>
        /// <typeparam name="TPayload">The type of the payload that should be stored.</typeparam>
        /// <param name="subscriptionId">The id of the subscription that generated the message from which the signal is received.</param>
        /// <param name="ruleId">The id of the rule, that generated the message from which the signal is received.</param>
        /// <param name="payload">The payload that should be stored in the signal store.</param>
        public void StoreValue<TPayload>(Guid subscriptionId, Guid ruleId, OtelMetric<TPayload> payload)
        {
            var key = this.GenerateKey(subscriptionId, ruleId);
            this.ValueStore[key] = payload;

            if (this.Callbacks.ContainsKey(key)) this.Callbacks[key](key);
        }

        /// <summary>
        /// Retrieves a value from the signal store.
        /// </summary>
        /// <typeparam name="TPayload">The type of the value.</typeparam>
        /// <param name="subscriptionId">The id of the subscription that generated the message from which the signal is received.</param>
        /// <param name="ruleId">The id of the rule, that generated the message from which the signal is received.</param>
        /// <returns>The value as the given type.</returns>
        /// <exception cref="Mqtt2OtelException">Thrown if the value cannot be cast to the given type.</exception>
        public OtelMetric<TPayload> GetValue<TPayload>(Guid subscriptionId, Guid ruleId)
        {
            var key = this.GenerateKey(subscriptionId, ruleId);

            if (!(this.ValueStore[key] is OtelMetric<TPayload>))
                throw new Mqtt2OtelException($"Cannot get value from {nameof(SignalStore)}. Key ({key}) returned an object of type {this.ValueStore[key].GetType().FullName}, but type {typeof(OtelMetric<TPayload>).FullName} was expected.");

            return (OtelMetric<TPayload>)this.ValueStore[key];
        }

        /// <summary>
        /// Tests if the store contains the given key.
        /// </summary>
        /// <param name="subscriptionId">The id of the subscription that generated the message from which the signal is received.</param>
        /// <param name="ruleId">The id of the rule, that generated the message from which the signal is received.</param>
        /// <returns>A value indicating whether the key exists inside the signal store.</returns>
        public bool ContainsKey(Guid subscriptionId, Guid ruleId)
        {
            var key = this.GenerateKey(subscriptionId, ruleId);

            return this.ValueStore.ContainsKey(key);
        }

        /// <summary>
        /// Updates a value. The key for the value must already exist.
        /// 
        /// Calls a callback function if registered.
        /// </summary>
        /// <typeparam name="TPayload">The type of the value to be updated.</typeparam>
        /// <param name="subscriptionId">The id of the subscription that generated the message from which the signal is received.</param>
        /// <param name="ruleId">The id of the rule, that generated the message from which the signal is received.</param>
        /// <param name="value">The new value.</param>
        /// <param name="attributes">Attributes that should be added to the metric value.</param>
        public void UpdateValue<TPayload>(Guid subscriptionId, Guid ruleId, TPayload value, IEnumerable<Variable> attributes)
        {
            var key = this.GenerateKey(subscriptionId, ruleId);
            var metric = this.GetValue<TPayload>(subscriptionId, ruleId);

            metric.Value = value;
            metric.Attributes = attributes;

            if (this.Callbacks.ContainsKey(key)) this.Callbacks[key](key);
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
        /// <param name="subscriptionId">The id of the subscription that generated the message from which the signal is received.</param>
        /// <param name="ruleId">The id of the rule, that generated the message from which the signal is received.</param>
        /// <returns>The generated key.</returns>
        private string GenerateKey(Guid subscriptionId, Guid ruleId)
        {
            return $"{subscriptionId}:{ruleId}";
        }
    }
}
