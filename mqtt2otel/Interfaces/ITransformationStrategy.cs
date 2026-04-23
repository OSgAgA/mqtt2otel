using mqtt2otel.Parser;
using System;
using System.Collections.Generic;
using System.Text;

namespace mqtt2otel.Interfaces
{
    /// <summary>
    /// Represents a strategy for applying transformations to a string payload.
    /// </summary>
    public interface ITransformationStrategy : IKeyObject
    {
        /// <summary>
        /// Apply the transformation to the payload.
        /// </summary>
        /// <param name="payload">The source payload.</param>
        /// <param name="pattern">A pattern describing the transformation to be applied.</param>
        /// <param name="context">The execution context in which the strategy will be exeucted.</param>
        /// <returns>The transformed payload.</returns>
        public string Apply(string payload, string pattern, ParsingContext context);
    }
}