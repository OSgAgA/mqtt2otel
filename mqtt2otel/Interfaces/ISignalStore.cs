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
        /// <param name="key">The key to test.</param>
        /// <returns>A value indicating whether the key exists inside the signal store.</returns>
        bool ContainsKey(string key);

        /// <summary>
        /// Deletes all entries from the store.
        /// </summary>
        void DeleteStore();

        /// <summary>
        /// Retrieves a value from the signal store.
        /// </summary>
        /// <typeparam name="TPayload">The type of the value.</typeparam>
        /// <param name="key">The key under which the value has been stored.</param>
        /// <returns>The value as the given type.</returns>
        /// <exception cref="Mqtt2OtelException">Thrown if the value cannot be cast to the given type.</exception>
        OtelMetric<TPayload> GetValue<TPayload>(string key);

        /// <summary>
        /// Register a callback function that will be called when a value with the given key is stored or updaten in the signal store.
        /// </summary>
        /// <param name="key">The key for which the callback should be called.</param>
        /// <param name="callback">The callback to be called. Will get the key as the argument.</param>
        void RegisterCallback(string key, Action<string> callback);

        /// <summary>
        /// Stores a value inside the signal store.
        /// 
        /// If registered a callback function will be called.
        /// </summary>
        /// <typeparam name="TPayload">The type of the payload that should be stored.</typeparam>
        /// <param name="key">The key under which the payload will be stored.</param>
        /// <param name="payload">The payload that should be stored in the signal store.</param>
        void StoreValue<TPayload>(string key, OtelMetric<TPayload> payload);

        /// <summary>
        /// Updates a value. The key for the value must already exist.
        /// 
        /// Calls a callback function if registered.
        /// </summary>
        /// <typeparam name="TPayload">The type of the value to be updated.</typeparam>
        /// <param name="key">The key under which the current value is stored.</param>
        /// <param name="value">The new value.</param>
        /// <param name="attributes">Attributes that should be added to the metric value.</param>
        void UpdateValue<TPayload>(string key, TPayload value, IEnumerable<Variable> attributes);
    }
}