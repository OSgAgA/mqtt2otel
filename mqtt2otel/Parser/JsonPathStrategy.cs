using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace mqtt2otel.Parser
{
    /// <summary>
    /// Represents a <see cref="IParsingStrategy"/> that is able to parse json payloads via JsonPath syntax.
    /// </summary>
    public class JsonPathStrategy : IParsingStrategy
    {
        /// <summary>
        /// The function name used by the strategy.
        /// </summary>
        public string Key => "JSONPATH";


        /// <summary>
        /// Parses the input as a json string.
        /// </summary>
        /// <param name="filter">A JsonPath expression (see <see cref="https://www.rfc-editor.org/rfc/rfc9535"/>) that will be applied to the payload.</param>
        /// <param name="context">The execution context in which the strategy will be exeucted.</param>
        /// <returns>The parsed payload.</returns>
        public object? Parse(string filter, ParsingContext context)
        {
           var jsonPayload = JObject.Parse(context.Message.Payload);
           var token = jsonPayload.SelectToken(filter);

            if (token == null)
            {
                throw new Exception($"Applying filter '{filter}' to payload '{context.Message.Payload}' returned no result.");
            }

            object? result = ((JValue)token).Value;

            if (result == null)
            {
                throw new Exception($"Result of applying filter: '{filter}' to payload '{context.Message.Payload}' could not be read.");
            }

            return result;
        }
    }
}
