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
        /// <param name="variableName">The variable name.</param>
        /// <param name="context">The execution context in which the strategy will be exeucted.</param>
        /// <returns>The parsed payload.</returns>
        public object? Parse(string variableName, ParsingContext context)
        {
            var query = context.Variables.Where(variable => variable.Key == variableName);

            if (!query.Any()) throw new Exception($"Could not find variable '{variableName}'.");

            return query.First().Value;
        }
    }
}
