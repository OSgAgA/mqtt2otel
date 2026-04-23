using System;
using System.Collections.Generic;
using System.Text;

namespace mqtt2otel.Parser
{
    public class VariableStrategy : IParsingStrategy
    {
        /// <summary>
        /// The function name used by the strategy.
        /// </summary>
        public string Key => "VAR";

        /// <summary>
        /// Returns the value of a variable with the given name.
        /// </summary>
        /// <typeparam name="T">The expected return type.</typeparam>
        /// <param name="payload">The input as json.</param>
        /// <param name="variableName">The variable name.</param>
        /// <param name="context">The execution context in which the strategy will be exeucted.</param>
        /// <returns>The parsed payload.</returns>
        public T Parse<T>(string payload, string variableName, ParsingContext context)
        {
            var query = context.Variables.Where(variable => variable.Key == variableName);

            if (!query.Any()) throw new Exception($"Could not find variable '{variableName}'.");

            return (T)query.First().Value;
        }
    }
}
