using System;
using System.Collections.Generic;
using System.Text;

namespace mqtt2otel.Parser
{
    /// <summary>
    /// Defines a strategy that will be used for parsing string payloads via the <see cref="PayloadParser"/>.
    /// 
    /// The strategy will be identified by its <see cref="IKeyObject.Key"/> property.
    /// </summary>
    public interface IParsingStrategy : IKeyObject
    {
        /// <summary>
        /// Parses the given input and applies a filter string.
        /// </summary>
        /// <typeparam name="T">The expected result type.</typeparam>
        /// <param name="payload">The payload.</param>
        /// <param name="filter">The filter that will be applied.</param>
        /// <returns>The filtered payload as the given type.</returns>
        public T Parse<T>(string payload, string filter);
    }
}
