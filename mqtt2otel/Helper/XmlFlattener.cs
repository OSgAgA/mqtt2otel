using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace mqtt2otel.Helper
{
    /// <summary>
    /// Provides helper methods for reading an XML string and flattening its elements and attributes.
    /// </summary>
    public static class XmlFlattener
    {
        /// <summary>
        /// Flattens the provided XML string.
        ///
        /// Example input:
        ///
        /// <Processor>
        ///   <TemperatureA>42</TemperatureA>
        ///   <TemperatureB>23</TemperatureB>
        /// </Processor>
        ///
        /// Output:
        ///
        /// Processor.TemperatureA = 42
        /// Processor.TemperatureB = 23
        /// </summary>
        /// <param name="xml">The original XML string.</param>
        /// <param name="separator">The separator used for combining names of different levels.</param>
        /// <returns>The XML as a flattened dictionary.</returns>
        public static Dictionary<string, object?> Flatten(string xml, string separator)
        {
            var root = XDocument.Parse(xml).Root
                ?? throw new ArgumentException("XML does not contain a root element.", nameof(xml));

            var result = new Dictionary<string, object?>();
            FlattenNode(root, result, prefix: root.Name.LocalName, separator);
            return result;
        }

        /// <summary>
        /// Recursively flattens an XML node.
        /// </summary>
        /// <param name="node">The XML node to be parsed.</param>
        /// <param name="result">The already produced result.</param>
        /// <param name="prefix">The current prefix.</param>
        /// <param name="separator">The separator used for combining names of different levels.</param>
        private static void FlattenNode(XElement node, Dictionary<string, object?> result, string prefix, string separator)
        {
            // Attributes
            foreach (var attr in node.Attributes())
            {
                var key = $"{prefix}{separator}@{attr.Name.LocalName}";
                result[key] = ConvertValue(attr.Value);
            }

            // Leaf node → store value
            if (!node.HasElements)
            {
                result[prefix] = ConvertValue(node.Value);
                return;
            }

            // Child elements
            foreach (var child in node.Elements())
            {
                var childPrefix = $"{prefix}{separator}{child.Name.LocalName}";
                FlattenNode(child, result, childPrefix, separator);
            }
        }

        /// <summary>
        /// Converts XML string values into int, double, bool, DateTime, or keeps string.
        /// </summary>
        private static object? ConvertValue(string raw)
        {
            if (int.TryParse(raw, out var i)) return i;
            if (double.TryParse(raw, out var d)) return d;
            if (bool.TryParse(raw, out var b)) return b;
            if (DateTime.TryParse(raw, out var dt)) return dt;

            return raw; // fallback: string
        }
    }
}
