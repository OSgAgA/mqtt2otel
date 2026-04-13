using MQTTnet.Extensions.ManagedClient;
using System;
using System.Collections.Generic;
using System.Text;

namespace mqtt2otel.Helper
{
    /// <summary>
    /// A helper class for parsing variables in strings.
    /// </summary>
    public static class VariableParser
    {
        /// <summary>
        /// Expand all variables that are found in a string. Variable names mus begin with a $.
        /// </summary>
        /// <example> Expand( "My lucky number is $luckyNumber", [ "luckyNumber", 42 ] => My lucky number is 42</example>
        /// <param name="text">The text that will be expanded.</param>
        /// <param name="variables">The variables that should be applied.</param>
        /// <returns>The expanded text.</returns>
        public static string Expand(string text, IEnumerable<Variable> variables)
        {
            foreach (var variable in variables)
            {
                text = text.Replace("$" + variable.Key, variable.Value.ToString()); ;
            }

            return text;
        }

        /// <summary>
        /// Expands all variables in source with the given replacements.
        /// </summary>
        /// <param name="source">A list of varialbes that should be expanded.</param>
        /// <param name="replacements">The replacements that will be used for expanding the source variables.</param>
        /// <returns>A new enumerable of expanded variables.</returns>
        public static IEnumerable<Variable> Expand(IEnumerable<Variable> source, IEnumerable<Variable> replacements)
        {
            var result = new List<Variable>();

            return source.Select(variable => new Variable() { Key = variable.Key, Value = VariableParser.Expand(variable.Value.ToString() ?? string.Empty, replacements) }).ToList();
        }
    }
}
