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
        private Dictionary<SignalKey, object> ValueStore = new();

        /// <summary>
        /// These callbacks will be executed when a value with the key of the dictionary is stored.
        /// </summary>
        private Dictionary<SignalKey, Action> Callbacks = new();

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
        public Action<OtelMeasurement, ParsingContext>? SignalCreator { get; set; } = null;

        /// <summary>
        /// Register a callback function that will be called when a value with the given key is stored or updaten in the signal store.
        /// </summary>
        /// <param name="measurement">The measurement to be stored.</param>
        /// <param name="callback">The callback to be called.</param>
        public void RegisterCallback(OtelMeasurement measurement, Action callback)
        {
            var key = new SignalKey(measurement.OtelConnection, measurement.SignalName);
            this.Callbacks[key] = callback;
        }

        /// <summary>
        /// Stores a value inside the signal store.
        /// 
        /// If registered a callback function will be called.
        /// </summary>
        /// <typeparam name="TPayload">The type of the payload that should be stored.</typeparam>
        /// <param name="measurement">The measurement to be stored.</param>
        /// <param name="payload">The payload that should be stored in the signal store.</param>
        public void StoreValue<TPayload>(OtelMeasurement measurement, OtelMetric<TPayload> payload)
        {
            var key = new SignalKey(measurement.OtelConnection, measurement.SignalName);
            this.ValueStore[key] = payload;

            if (this.Callbacks.TryGetValue(key, out var callback)) callback();
        }

        /// <summary>
        /// Retrieves a value from the signal store.
        /// </summary>
        /// <typeparam name="TPayload">The type of the value.</typeparam>
        /// <param name="measurement">The measurement to be stored.</param>
        /// <returns>The value as the given type.</returns>
        /// <exception cref="Mqtt2OtelException">Thrown if the value cannot be cast to the given type.</exception>
        public OtelMetric<TPayload> GetValue<TPayload>(OtelMeasurement measurement)
        {
            var key = new SignalKey(measurement.OtelConnection, measurement.SignalName);

            if (this.ValueStore[key] is not OtelMetric<TPayload>)
                throw new Mqtt2OtelException($"Cannot get value from {nameof(SignalStore)}. Key ({key}) returned an object of type {this.ValueStore[key].GetType().FullName}, but type {typeof(OtelMetric<TPayload>).FullName} was expected.");

            return (OtelMetric<TPayload>)this.ValueStore[key];
        }

        /// <summary>
        /// Tests if the store contains the given key.
        /// </summary>
        /// <param name="measurement">The measurement to be stored.</param>
        /// <returns>A value indicating whether the key exists inside the signal store.</returns>
        public bool ContainsKey(OtelMeasurement measurement)
        {
            var key = new SignalKey(measurement.OtelConnection, measurement.SignalName);

            return this.ValueStore.ContainsKey(key);
        }

        /// <summary>
        /// Updates a value. 
        /// 
        /// Calls a callback function if registered.
        /// </summary>
        /// <typeparam name="TPayload">The type of the value to be updated.</typeparam>
        /// <param name="measurement">The measurement to be stored.</param>
        /// <param name="context">The current parsing context.</param>
        public void UpdateValue<TPayload>(OtelMeasurement measurement, ParsingContext context)
        {
            if (measurement.Value == null) return;

            var key = new SignalKey(measurement.OtelConnection, measurement.SignalName);

            if (this.SignalCreator != null && !this.ContainsKey(measurement)) this.SignalCreator(measurement, context);
            var metric = this.GetValue<TPayload>(measurement);

            metric.Value = (TPayload)measurement.Value;
            metric.Attributes = measurement.Attributes;

            if (this.Callbacks.TryGetValue(key, out var callback)) callback();
        }

        /// <summary>
        /// Deletes all entries from the store.
        /// </summary>
        public void DeleteStore()
        {
            this.ValueStore.Clear();
            this.Callbacks.Clear();
        }
    }
}
