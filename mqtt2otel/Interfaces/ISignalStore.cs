using mqtt2otel.Manifest;
using mqtt2otel.Parser;

namespace mqtt2otel.Interfaces
{
    /// <summary>
    /// Stores metric signals to be delivered to an open telemetry endpoint.
    /// </summary>
    public interface ISignalStore
    {
        /// <summary>
        /// Tests if the store contains the given key.
        /// </summary>
        /// <param name="subscriptionId">The id of the subscription that generated the message from which the signal is received.</param>
        /// <param name="ruleId">The id of the rule, that generated the message from which the signal is received.</param>
        /// <param name="signalName">The name of the signal that will be created (if not allready existing).</param>
        /// <returns>A value indicating whether the key exists inside the signal store.</returns>
        public bool ContainsKey(Guid subscriptionId, Guid ruleId, string signalName);

        /// <summary>
        /// Deletes all entries from the store.
        /// </summary>
        void DeleteStore();

        /// <summary>
        /// Retrieves a value from the signal store.
        /// </summary>
        /// <typeparam name="TPayload">The type of the value.</typeparam>
        /// <param name="subscriptionId">The id of the subscription that generated the message from which the signal is received.</param>
        /// <param name="ruleId">The id of the rule, that generated the message from which the signal is received.</param>
        /// <param name="signalName">The name of the signal that will be created (if not allready existing).</param>
        /// <returns>The value as the given type.</returns>
        /// <exception cref="Mqtt2OtelException">Thrown if the value cannot be cast to the given type.</exception>
        public OtelMetric<TPayload> GetValue<TPayload>(Guid subscriptionId, Guid ruleId, string signalName);

        /// <summary>
        /// Register a callback function that will be called when a value with the given key is stored or updaten in the signal store.
        /// </summary>
        /// <param name="subscriptionId">The id of the subscription that generated the message from which the signal is received.</param>
        /// <param name="ruleId">The id of the rule, that generated the message from which the signal is received.</param>
        /// <param name="signalName">The name of the signal that will be created (if not allready existing).</param>
        /// <param name="callback">The callback to be called.</param>
        public void RegisterCallback(Guid subscriptionId, Guid ruleId, string signalName, Action callback);

        /// <summary>
        /// Stores a value inside the signal store.
        /// 
        /// If registered a callback function will be called.
        /// </summary>
        /// <typeparam name="TPayload">The type of the payload that should be stored.</typeparam>
        /// <param name="subscription">The subscription that generated the message from which the signal is received.</param>
        /// <param name="rule">The rule, that generated the message from which the signal is received.</param>
        /// <param name="name">The name of the signal that will be created (if not allready existing).</param>
        /// <param name="payload">The payload that should be stored in the signal store.</param>
        public void StoreValue<TPayload>(MqttSubscription subscription, OtelMetricRule rule, string name, OtelMetric<TPayload> payload);

        /// <summary>
        /// Updates a value. The key for the value must already exist.
        /// 
        /// Calls a callback function if registered.
        /// </summary>
        /// <typeparam name="TPayload">The type of the value to be updated.</typeparam>
        /// <param name="subscription">The subscription that generated the message from which the signal is received.</param>
        /// <param name="rule">The rule, that generated the message from which the signal is received.</param>
        /// <param name="context">The current parsing context.</param>
        /// <param name="signalName">The name of the signal that will be created (if not allready existing).</param>
        /// <param name="signalType">The type of the signal.</param>
        /// <param name="value">The new value.</param>
        /// <param name="attributes">Attributes that should be added to the metric value.</param>
        public void UpdateValue<TPayload>(MqttSubscription subscription, OtelMetricRule rule, string signalName, SignalDataType signalType, ParsingContext context, TPayload value, IEnumerable<OtelAttribute> attributes);

        /// <summary>
        /// This action is called when no signal is found and a new signal for the given parameters must be created.
        /// </summary>
        public Action<MqttSubscription, OtelMetricRule, string, SignalDataType, ParsingContext>? SignalCreator { get; set; }
    }
}