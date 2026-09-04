using mqtt2otel.Manifest;
using mqtt2otel.Helper;
using System;
using System.Collections.Generic;
using System.Text;

namespace mqtt2otel.Parser
{
    /// <summary>
    /// Represents a <see cref="IParsingStrategy"/> that is able to provide constant values.
    /// </summary>
    public class ConstantValueStrategy : IParsingStrategy
    {
        /// <summary>
        /// The function name used by the strategy.
        /// </summary>
        public string Key => "CONST";

        /// <summary>
        /// Returns the filter as the given type.
        /// </summary>
        /// <param name="filter">The value that will be returned.</param>
        /// <param name="context">The execution context in which the strategy will be exeucted.</param>
        /// <returns>The original filter value.</returns>
        public object? Parse(string filter, ParsingContext context)
        {
            return filter;
        }
    }
}
