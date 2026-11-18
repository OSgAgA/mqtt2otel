using System;
using System.Collections.Generic;
using System.Text;

namespace mqtt2otel.Interfaces
{
    /// <summary>
    /// Represents a provider class that delivers date and time values.
    /// </summary>
    public interface IDateTimeProvider
    {
        /// <summary>
        /// Gets the current date and time as a UTC timestamp.
        /// </summary>
        /// <returns>The current date and time as a UTC timestamp.</returns>
        public DateTime GetUtcNow();
    }
}
