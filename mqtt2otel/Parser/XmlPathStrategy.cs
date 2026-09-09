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
        /// <param name="filter">A XPath expression (see <see cref="https://www.w3.org/TR/xpath-31/"/>) that will be applied to the payload.</param>
        /// <param name="context">The execution context in which the strategy will be exeucted.</param>
        /// <returns>The parsed payload.</returns>
        public object? Parse(string filter, ParsingContext context)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(context.Message.Payload);

            var result = doc.SelectSingleNode(filter)?.InnerXml;

            if (result != null)
            {
                if (long.TryParse(result, out var i)) return i;
                if (double.TryParse(result, out var d)) return d;
                if (bool.TryParse(result, out var b)) return b;
                if (DateTime.TryParse(result, out var dt)) return dt;

                return result;
            }

            return null;
        }
    }
}
