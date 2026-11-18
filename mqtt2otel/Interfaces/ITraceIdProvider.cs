using mqtt2otel.Provider;
using System;
using System.Collections.Generic;
using System.Text;

namespace mqtt2otel.Interfaces
{
    /// <summary>
    /// Represents classes that can provie trace and span ids.
    /// </summary>
    public interface ITraceIdProvider
    {
        /// <summary>
        /// Creates a new span id.
        /// </summary>
        /// <returns>The newly created id.</returns>
        public string CreateSpanId();

        /// <summary>
        /// Creates a new trace id.
        /// </summary>
        /// <returns>The newly created id.</returns>
        public string CreateTraceId();

        /// <summary>
        /// Gets the current state.
        /// </summary>
        /// <returns>The current provider state.</returns>
        public ProviderState GetState();
    }
}
