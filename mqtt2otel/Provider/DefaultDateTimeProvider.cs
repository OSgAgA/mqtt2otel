using mqtt2otel.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace mqtt2otel.Provider
{
    /// <summary>
    /// The default provider for date and time values.
    /// </summary>
    public class DefaultDateTimeProvider : IDateTimeProvider
    {
        /// <summary>
        /// Gets the current date and time as a UTC timestamp.
        /// </summary>
        /// <returns>The current date and time as a UTC timestamp.</returns>
        public DateTime GetUtcNow()
        {
            return DateTime.UtcNow;
        }
    }
}
