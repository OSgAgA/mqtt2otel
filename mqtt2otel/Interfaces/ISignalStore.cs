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
        /// <param name="measurement">The measurement to be stored.</param>
        /// <returns>A value indicating whether the key exists inside the signal store.</returns>
        public bool ContainsKey(OtelMeasurement measurement);

        /// <summary>
        /// Deletes all entries from the store.
        /// </summary>
        void DeleteStore();

        /// <summary>
        /// Retrieves a value from the signal store.
        /// </summary>
        /// <typeparam name="TPayload">The type of the value.</typeparam>
        /// <param name="measurement">The measurement to be stored.</param>
        /// <returns>The value as the given type.</returns>
        /// <exception cref="Mqtt2OtelException">Thrown if the value cannot be cast to the given type.</exception>
        public OtelMetric<TPayload> GetValue<TPayload>(OtelMeasurement measurement);

        /// <summary>
        /// Register a callback function that will be called when a value with the given key is stored or updaten in the signal store.
        /// </summary>
        /// <param name="measurement">The measurement to be stored.</param>
        /// <param name="callback">The callback to be called.</param>
        public void RegisterCallback(OtelMeasurement measurement, Action callback);

        /// <summary>
        /// Stores a value inside the signal store.
        /// 
        /// If registered a callback function will be called.
        /// </summary>
        /// <typeparam name="TPayload">The type of the payload that should be stored.</typeparam>
        /// <param name="measurement">The measurement to be stored.</param>
        /// <param name="payload">The payload that should be stored in the signal store.</param>
        public void StoreValue<TPayload>(OtelMeasurement measurement, OtelMetric<TPayload> payload);

       /// <summary>
       /// Updates a value. 
       /// 
       /// Calls a callback function if registered.
       /// </summary>
       /// <typeparam name="TPayload">The type of the value to be updated.</typeparam>
       /// <param name="measurement">The measurement to be stored.</param>
       /// <param name="context">The current parsing context.</param>
        public void UpdateValue<TPayload>(OtelMeasurement measurement, ParsingContext context);

        /// <summary>
        /// This action is called when no signal is found and a new signal for the given parameters must be created.
        /// </summary>
        public Action<OtelMeasurement, ParsingContext>? SignalCreator { get; set; }
    }
}