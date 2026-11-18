using mqtt2otel.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace mqtt2otel.Provider
{
    /// <summary>
    /// A fake date time provider that provides determinstic date/time values for testing.
    /// </summary>
    public class FakeDateTimeProvider : IDateTimeProvider
    {
        /// <summary>
        /// The start time.
        /// </summary>
        private static DateTime Start = new DateTime(2026, 01, 01, 10, 00, 42);

        /// <summary>
        /// The amount of seconds that will be added to the start time.
        /// </summary>
        private static int index = 0;

        /// <summary>
        /// Gets the current date and time as a UTC timestamp.
        /// </summary>
        /// <returns>The current date and time as a UTC timestamp.</returns>
        public DateTime GetUtcNow()
        {
            return Start.AddSeconds(index++);
        }

        /// <summary>
        /// Resets the provider.
        /// </summary>
        public static void Reset()
        {
            index = 0;
        }
    }
}
