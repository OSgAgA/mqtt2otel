using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace mqtt2otel.Helper
{

    /// <summary>
    /// Static extension methods for working with attribute lists.
    /// </summary>
    public static class OtelAttributeExtensions
    {
        /// <summary>
        /// Combines two attribute enumerables to one.
        /// </summary>
        /// <param name="a">The fist enumerable.</param>
        /// <param name="b">The second enumerable.</param>
        /// <returns>The combined list.</returns>
        public static IEnumerable<OtelAttribute> Combine(this IEnumerable<OtelAttribute> a, IEnumerable<OtelAttribute> b)
        {
            var combined = new List<OtelAttribute>();

            combined.AddRange(a.ToList());
            combined.AddRange(b.ToList());

            return combined;
        }

        /// <summary>
        /// Converts an <see cref="IEnumerable{OtelAttribute}"/> to an <see cref="IEnumerable{KeyValuePair{String, Object}}"/>.
        /// </summary>
        /// <param name="source">the source.</param>
        /// <returns>The converted enumerable.</returns>
        public static IEnumerable<KeyValuePair<string, object?>> ToKeyValuePairs(this IEnumerable<OtelAttribute> source)
        {
            return source.Select(x => new KeyValuePair<string, object?>(x.Key, x.Value));
        }

        /// <summary>
        /// Converts an <see cref="IEnumerable{OtelAttribute}"/> to an <see cref="TagList"/>.
        /// </summary>
        /// <param name="source">the source.</param>
        /// <returns>The converted enumerable.</returns>
        public static TagList ToTagList(this IEnumerable<OtelAttribute> input)
        {
            var result = new TagList();

            foreach (var variable in input)
            {
                result.Add(variable.Key, variable.Value);
            }

            return result;
        }
    }
}
