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
        /// <typeparam name="T">The expected return type.</typeparam>
        /// <param name="payload">Will be ignored.</param>
        /// <param name="filter">The value that will be returned.</param>
        /// <param name="context">The execution context in which the strategy will be exeucted.</param>
        /// <returns>The parsed filter value.</returns>
        public T Parse<T>(string payload, string filter, ParsingContext context)
        {
            return TypeHelper.Parse<T>(filter);
        }
    }
}
