using Microsoft.Extensions.Logging;
using mqtt2otel.Stores;

namespace mqtt2otel.Interfaces
{
    /// <summary>
    /// A class that stores open telemetry logger and make them accessable to consumers.
    /// </summary>
    public interface ILoggerStore
    {
        /// <summary>
        /// Checks if a key exists in the store.
        /// </summary>
        /// <param name="key">The key to be checked.</param>
        /// <returns>A value indicating whether the key exists in the store, or not.</returns>
        bool ContainsKey(Guid key);

        /// <summary>
        /// Deletes all entries from the store.
        /// </summary>
        void DeleteStore();

        /// <summary>
        /// Gets the logger with the given key. Throws an exception if the key was not found.
        /// Please use <see cref="ContainsKey(Guid)"/> to check for existance.
        /// </summary>
        /// <param name="key">The key under which the logger has been stored.</param>
        /// <returns>The logger with the given key.</returns>
        OtelLogger GetLogger(Guid key);

        /// <summary>
        /// Stores a logger inside the store.
        /// </summary>
        /// <param name="key">The key that identifies the logger for furhter access.</param>
        /// <param name="logger">The logger to be added.</param>
        /// <exception cref="Mqtt2OtelException">Thrown if the key allready exists.</exception>
        void StoreLogger(Guid key, ILogger logger);
    }
}