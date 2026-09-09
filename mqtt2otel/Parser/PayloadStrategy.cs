using Newtonsoft.Json.Linq;
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
        /// Parses the payload and tries to infer its datatype.
        /// 
        /// If the regular expression returns more than one match, then the first match is used.
        /// </summary>
        /// <typeparam name="T">Must be string.</typeparam>
        /// <param name="filter">Will be ignored.</param>
        /// <param name="context">The execution context in which the strategy will be exeucted.</param>
        /// <returns>The parsed payload.</returns>
        /// <exception cref="Exception">Thrown if generic return type is not a string.</exception>
        public object? Parse(string filter, ParsingContext context)
        {
            if (long.TryParse(context.Message.Payload, out var i)) return i;
            if (double.TryParse(context.Message.Payload, out var d)) return d;
            if (bool.TryParse(context.Message.Payload, out var b)) return b;
            if (DateTime.TryParse(context.Message.Payload, out var dt)) return dt;

            return context.Message.Payload; 
        }
    }
}
