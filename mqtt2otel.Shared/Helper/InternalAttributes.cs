using System;
using System.Collections.Generic;
using System.Text;

namespace mqtt2otel.Shared.Helper
{
    /// <summary>
    /// Contains the names of internal attributes. These attributes exist for technical reasons
    /// only and will be removed from the final result.
    /// </summary>
    public static class InternalAttributes
    {
        /// <summary>
        /// The prefix, that will be used for internal attributes.
        /// </summary>
        public const string InternalAttributePrefix = "__mqtt2otel__internal__";

        /// <summary>
        /// The id used to identify an the exporter route for routing inside an otel collector config.
        /// </summary>
        public const string ExporterRoutingId = InternalAttributePrefix + "exporter_route";

        /// <summary>
        /// The trace id of an otel trace.
        /// </summary>
        public const string TraceId = InternalAttributePrefix + "trace_id";

        /// <summary>
        /// The span id of an otel span.
        /// </summary>
        public const string SpanId = InternalAttributePrefix + "span_id";

        /// <summary>
        /// Gets an internal attribute from a list of key value pairs.
        /// </summary>
        /// <param name="tags">The tags.</param>
        /// <param name="attribute">The attribute name without the prefix.</param>
        /// <returns>The string value if found or an empty string.</returns>
        public static string GetValueFromTags(IEnumerable<KeyValuePair<string, string?>> tags, string attribute)
        {
            string prefixedName = attribute;
            var query = tags.Where(tag => tag.Key == prefixedName);

            if (query.Any()) return query.First().Value ?? string.Empty;

            return string.Empty;
        }
    }
}
