using System;
using System.Collections.Generic;
using System.Text;

namespace mqtt2otel.Transformation
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
        /// <returns>The transformed payload.</returns>
        public string Apply(string payload, string pattern);
    }
}
