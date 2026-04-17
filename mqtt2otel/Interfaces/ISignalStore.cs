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
        /// <returns>A value indicating whether the key exists inside the signal store.</returns>
        public bool ContainsKey(Guid subscriptionId, Guid ruleId);

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
        /// <returns>The value as the given type.</returns>
        /// <exception cref="Mqtt2OtelException">Thrown if the value cannot be cast to the given type.</exception>
        public OtelMetric<TPayload> GetValue<TPayload>(Guid subscriptionId, Guid ruleId);

        /// <summary>
        /// Register a callback function that will be called when a value with the given key is stored or updaten in the signal store.
        /// </summary>
        /// <param name="subscriptionId">The id of the subscription that generated the message from which the signal is received.</param>
        /// <param name="ruleId">The id of the rule, that generated the message from which the signal is received.</param>
        /// <param name="callback">The callback to be called. Will get the key as the argument.</param>
        public void RegisterCallback(Guid subscriptionId, Guid ruleId, Action<string> callback);

        /// <summary>
        /// Stores a value inside the signal store.
        /// 
        /// If registered a callback function will be called.
        /// </summary>
        /// <typeparam name="TPayload">The type of the payload that should be stored.</typeparam>
        /// <param name="subscriptionId">The id of the subscription that generated the message from which the signal is received.</param>
        /// <param name="ruleId">The id of the rule, that generated the message from which the signal is received.</param>
        /// <param name="payload">The payload that should be stored in the signal store.</param>
        public void StoreValue<TPayload>(Guid subscriptionId, Guid ruleId, OtelMetric<TPayload> payload);

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
        public void UpdateValue<TPayload>(Guid subscriptionId, Guid ruleId, TPayload value, IEnumerable<Variable> attributes);
    }
}