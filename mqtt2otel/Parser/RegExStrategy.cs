using mqtt2otel.Helper;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace mqtt2otel.Parser
{
    /// <summary>
    /// Represents a <see cref="IParsingStrategy"/> that is able to parse strings via regular expressions.
    /// </summary>
    public class RegExStrategy : IParsingStrategy
    {
        /// <summary>
        /// The function name used by the strategy.
        /// </summary>
        public string Key => "REGEX";


        /// <summary>
        /// Parses the payload via applying a regular expressin.
        /// 
        /// If the regular expression returns more than one match, then the first match is used.
        /// </summary>
        /// <param name="filter">A RegEx expression (see <see cref="https://learn.microsoft.com/en-us/dotnet/standard/base-types/regular-expression-language-quick-reference"/>) that will be applied to the payload.</param>
        /// <param name="context">The execution context in which the strategy will be exeucted.</param>
        /// <returns>The parsed payload.</returns>
        public object? Parse(string filter, ParsingContext context)
        {
            var regex = new Regex(filter);

            var match = regex.Match(context.Message.Payload);

            if (match.Success)
            {
                string valueAsString = match.Value;

                if (match.Groups.Count > 1)
                {
                    valueAsString = match.Groups[1].Value;
                }

                return valueAsString;
            }

            return string.Empty;
        }
    }
}
