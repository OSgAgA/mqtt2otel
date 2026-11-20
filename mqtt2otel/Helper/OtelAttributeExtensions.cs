using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

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
        /// Combines two attribute lists to one.
        /// </summary>
        /// <param name="a">The fist enumerable.</param>
        /// <param name="b">The second enumerable.</param>
        /// <param name="overwriteExising">A value indicating whether values of b, that are already existing in a, should not be added, but override the existing value.</param>
        /// <returns>The combined list.</returns>
        public static List<OtelAttribute> Combine(this List<OtelAttribute> a, List<OtelAttribute> b, bool overwriteExising = false)
        {
            var combined = new List<OtelAttribute>();

            combined.AddRange(a.ToList());

            if (overwriteExising)
            {
                foreach (var item in b)
                {
                    var query = combined.Where(attr => attr.Key == item.Key);
                    if (query.Any())
                    {
                        query.First().Value = item.Value;
                    }
                    else
                    {
                        combined.Add(item);
                    }
                }
            }
            else
            {
                combined.AddRange(b.ToList());
            }

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
        /// Converts an  <see cref="IEnumerable{KeyValuePair{String, Object}}"/> to an <see cref="IEnumerable{OtelAttribute}"/>.
        /// </summary>
        /// <param name="source">the source.</param>
        /// <returns>The converted enumerable.</returns>
        public static List<OtelAttribute> ToOtelAttributes<T>(this IEnumerable<KeyValuePair<string, T>> source)
        {
            var result = new List<OtelAttribute>();

            foreach (var pair in source)
            {
                if (pair.Value != null) result.Add(new OtelAttribute(pair.Key, pair.Value));
            }

            return result;
        }

        /// <summary>
        /// Converts an enumerable of attributes into a dictionary.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <returns>The converted enumerable.</returns>
        public static Dictionary<string, T> ToAttributeDictionary<T>(this IEnumerable<OtelAttribute> source)
        {
            var result = new Dictionary<string, T>();

            foreach (var pair in source)
            {
                result.Add(pair.Key, (T)pair.Value);
            }

            return result;
        }

        /// <summary>
        /// Converts an enumerable of attributes into a dictionary.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <returns>The converted enumerable.</returns>
        public static Dictionary<string, string?> ToAttributeStringDictionary(this IEnumerable<OtelAttribute> source)
        {
            var result = new Dictionary<string, string?>();

            foreach (var pair in source)
            {
                if (pair.Value != null) result.Add(pair.Key, pair.Value.ToString() ?? string.Empty);
            }

            return result;
        }

        /// <summary>
        /// Converts an <see cref="IEnumerable{OtelAttribute}"/> to an <see cref="ActivityTagsCollection"/>.
        /// </summary>
        /// <param name="source">the source.</param>
        /// <returns>The converted enumerable.</returns>
        public static ActivityTagsCollection ToActivityTagsCollection(this IEnumerable<OtelAttribute> source)
        {
            return new ActivityTagsCollection(source.ToKeyValuePairs());
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

        /// <summary>
        /// Adds an internal attribute to the list.
        /// </summary>
        /// <param name="attributes">The attribute lists.</param>
        /// <param name="key">The key of the internal attribute without the internal attribute prefix.</param>
        /// <param name="value">The value of the attribute as a string.</param>
        internal static void AddInternalAttribute(this List<OtelAttribute> attributes, string key, string value)
        {
            var attribute = OtelAttribute.CreateInternalAttribute(key, value);
            attributes.Add(attribute);
        }
    }
}
