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
        /// <param name="context">The execution context in which the strategy will be exeucted.</param>
        /// <returns>The parsed payload.</returns>
        /// <exception cref="Exception">Thrown if generic return type is not a string.</exception>
        public T Parse<T>(string input, string filter, ParsingContext context)
        {
            object result = string.Empty;

            if (typeof(T) == typeof(int))
            {
                result = int.Parse(input);
            }
            else if (typeof(T) == typeof(float))
            {
                result = float.Parse(input);
            }
            else if (typeof(T) == typeof(double))
            {
                result = double.Parse(input);
            }
            else if (typeof(T) == typeof(long))
            {
                result = long.Parse(input);
            }
            else if (typeof(T) == typeof(decimal))
            {
                result = decimal.Parse(input);
            }
            else
            {
                result = input;
            }

            return (T)result;
        }
    }
}
