using Microsoft.Extensions.Logging;
using mqtt2otel.Manifest;
using mqtt2otel.Helper;
using mqtt2otel.Parser;
using System;
using System.Collections.Generic;
using System.Text;

namespace mqtt2otel.Stores
{
    /// <summary>
    /// A class that stores open telemetry logger and make them accessable to consumers.
    /// </summary>
    public class LoggerStore
    {
        /// <summary>
        /// Contains the stored open telemetry loggers and their id.
        /// </summary>
        private Dictionary<Guid, OtelLogger> store = new ();

        /// <summary>
        /// A payload parser that will be given to the stored <see cref="OtelLogger"/> to parse payloads.
        /// </summary>
        private readonly PayloadParser payloadParser;

        /// <summary>
        /// Initializes a new instance of the <see cref="LoggerStore"/> class.
        /// </summary>
        /// <param name="payloadParser">A payload parser that will be provided to all stored loggers.</param>
        public LoggerStore(PayloadParser payloadParser)
        {
            this.payloadParser = payloadParser;
        }

        /// <summary>
        /// Stores a logger inside the store.
        /// </summary>
        /// <param name="key">The key that identifies the logger for furhter access.</param>
        /// <param name="logger">The logger to be added.</param>
        /// <exception cref="Mqtt2OtelException">Thrown if the key allready exists.</exception>
        public void StoreLogger(Guid key, ILogger logger)
        {
            if (this.store.ContainsKey(key))
                throw new Mqtt2OtelException($"Cannot add logger with key {key} to store, as the key allready exists.");
            
            this.store[key] = new OtelLogger(logger, payloadParser);
        }

        /// <summary>
        /// Gets the logger with the given key. Throws an exception if the key was not found.
        /// Please use <see cref="ContainsKey(Guid)"/> to check for existance.
        /// </summary>
        /// <param name="key">The key under which the logger has been stored.</param>
        /// <returns>The logger with the given key.</returns>
        public OtelLogger GetLogger(Guid key)
        {
            return this.store [key]; 
        }

        /// <summary>
        /// Checks if a key exists in the store.
        /// </summary>
        /// <param name="key">The key to be checked.</param>
        /// <returns>A value indicating whether the key exists in the store, or not.</returns>
        public bool ContainsKey(Guid key)
        {
            return this.store.ContainsKey(key);
        }

        /// <summary>
        /// Deletes all entries from the store.
        /// </summary>
        public void DeleteStore()
        {
            this.store.Clear();
        }
    }
}
