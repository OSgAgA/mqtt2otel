using mqtt2otel.Helper;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace mqtt2otel.Parser
{
    /// <summary>
    /// Represents a <see cref="IParsingStrategy"/> that is able to parse xml payloads via XPath syntax.
    /// </summary>
    public class XmlPathStrategy : IParsingStrategy
    {
        /// <summary>
        /// The function name used by the strategy.
        /// </summary>
        public string Key => "XMLPATH";

        /// <summary>
        /// Parses the input as a xml string.
        /// </summary>
        /// <typeparam name="T">The expected return type.</typeparam>
        /// <param name="payload">The input as xml.</param>
        /// <param name="filter">A XPath expression (see <see cref="https://www.w3.org/TR/xpath-31/"/>) that will be applied to the payload.</param>
        /// <returns>The parsed payload.</returns>
        public T Parse<T>(string payload, string filter)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(payload);

            var result = doc.SelectSingleNode(filter)?.InnerXml;

            if (result != null) return TypeHelper.Parse<T>(result);

            var d = default(T);

            if (d != null) return d;

            throw new Exception($"Could not process xml path expression {filter}.");
        }
    }
}
