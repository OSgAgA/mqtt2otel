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
        /// <param name="filter">Will be ignored.</param>
        /// <param name="context">The execution context in which the strategy will be exeucted.</param>
        /// <returns>The parsed payload.</returns>
        /// <exception cref="Exception">Thrown if generic return type is not a string.</exception>
        public T Parse<T>(string filter, ParsingContext context)
        {
            object result = string.Empty;

            if (typeof(T) == typeof(int))
            {
                result = int.Parse(context.Message.Payload);
            }
            else if (typeof(T) == typeof(float))
            {
                result = float.Parse(context.Message.Payload);
            }
            else if (typeof(T) == typeof(double))
            {
                result = double.Parse(context.Message.Payload);
            }
            else if (typeof(T) == typeof(long))
            {
                result = long.Parse(context.Message.Payload);
            }
            else if (typeof(T) == typeof(decimal))
            {
                result = decimal.Parse(context.Message.Payload);
            }
            else
            {
                result = context.Message.Payload;
            }

            return (T)result;
        }
    }
}
