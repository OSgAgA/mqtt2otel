using mqtt2otel.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace mqtt2otel.Provider
{
    /// <summary>
    /// The default trace id provider class.
    /// </summary>
    public class DefaultTraceIdProvider : ITraceIdProvider
    {
        /// <summary>
        /// Creates a new span id.
        /// </summary>
        /// <returns>The newly created id.</returns>
        public string CreateSpanId()
        {
            return ActivitySpanId.CreateRandom().ToString();
        }

        /// <summary>
        /// Creates a new trace id.
        /// </summary>
        /// <returns>The newly created id.</returns>
        public string CreateTraceId()
        {
            return ActivityTraceId.CreateRandom().ToString();
        }

        /// <summary>
        /// Gets the current state.
        /// </summary>
        /// <returns>The current provider state.</returns>
        public ProviderState GetState() => ProviderState.Generate(new object[] { });
    }
}
