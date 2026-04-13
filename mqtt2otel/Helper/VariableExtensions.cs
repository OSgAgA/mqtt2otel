using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Linq;

namespace mqtt2otel.Helper
{
    /// <summary>
    /// Static extension methods for working with variables.
    /// </summary>
    public static class VariableExtensions
    {
        /// <summary>
        /// Converts an <see cref="IEnumerable{Variable}"/> to an <see cref="IEnumerable{KeaValuepair{String, Object}}"/>.
        /// </summary>
        /// <param name="source">the source.</param>
        /// <returns>The converted enumerable.</returns>
        public static IEnumerable<KeyValuePair<string, object?>> ToKeyValuePairs(this IEnumerable<Variable> source)
        {
            return source.Select(x => new KeyValuePair<string, object?>(x.Key, x.Value));
        }

        /// <summary>
        /// Converts an <see cref="IEnumerable{Variable}"/> to an <see cref="TagList"/>.
        /// </summary>
        /// <param name="source">the source.</param>
        /// <returns>The converted enumerable.</returns>
        public static TagList ToTagList(this IEnumerable<Variable> input)
        {
            var result = new TagList();

            foreach (var variable in input)
            {
                result.Add(variable.Key, variable.Value);
            }

            return result;
        }

        /// <summary>
        /// Combines to variable lists to one.
        /// </summary>
        /// <param name="a">The fist list.</param>
        /// <param name="b">The second list.</param>
        /// <returns>The combined list.</returns>
        public static IEnumerable<Variable> Combine(this IEnumerable<Variable> a, IEnumerable<Variable> b)
        {
            var combined = new List<Variable>();

            combined.AddRange(a.ToList());
            combined.AddRange(b.ToList());

            return combined;
        }
    }
}
