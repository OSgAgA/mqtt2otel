using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace mqtt2otel.Helper
{
    public static class DateTimeHelper
    {
        /// <summary>
        /// Tries to parse a string as a date and time with utc as the given default timezone. If no explicit time offset is provided the time 
        /// is interpreted as utc, otherwise the explict setting is respected.
        /// 
        /// Result is always converted to UTC. If parsing was not successful null is returned.
        /// </summary>
        /// <example>
        ///   * "2026-11-08T12:34:56" → treated as UTC (no offset present) → 2026-11-08T12:34:56Z.
        ///   * "2026-11-08T12:34:56+02:00" → offset respected → converted to 2026-11-08T10:34:56Z.
        ///   * "2026-11-08T12:34:56Z" → respected as UTC.
        /// </example>
        /// <param name="input">The input string that should be parsed to a date time.</param>
        /// <returns></returns>
        public static bool TryParseToUtc(string input, out DateTime result)
        {
            if (DateTimeOffset.TryParse(input, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var dto))
            {
                result = dto.UtcDateTime;
                return true;
            }

            result = default;
            return false;
        }
    }
}
