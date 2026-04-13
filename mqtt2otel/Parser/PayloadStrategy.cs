using System;
using System.Collections.Generic;
using System.Text;

namespace mqtt2otel.Parser
{
    /// <summary>
    /// Represents a <see cref="IParsingStrategy"/> that is able to parse strings as plain text.
    /// </summary>

    internal class TextStrategy : IParsingStrategy
    {
        /// <summary>
        /// The function name used by the strategy.
        /// </summary>
        public string Key => "PAYLOAD";

        /// <summary>
        /// Parses the payload via returning it as plain text.
        /// 
        /// If the regular expression returns more than one match, then the first match is used.
        /// </summary>
        /// <typeparam name="T">Must be string.</typeparam>
        /// <param name="payload">The payload.</param>
        /// <param name="filter">Will be ignored.</param>
        /// <returns>The parsed payload.</returns>
        /// <exception cref="Exception">Thrown if generic return type is not a string.</exception>
        public T Parse<T>(string input, string filter)
        {
            if (typeof(T) != typeof(string))
            {
                throw new Exception($"Text strategy only supports string type, but {typeof(T).FullName} was provided.");
            }

            return (T)(object)input;
        }
    }
}
