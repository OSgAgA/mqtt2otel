using System;
using System.Collections.Generic;
using System.Text;

namespace mqtt2otel.Helper
{
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Provides helper methods for reading a json string and flattening it's properties.
    /// </summary>
    public static class JsonFlattener
    {
        /// <summary>
        /// Flattens the provided string.
        /// 
        /// If given a json:
        /// 
        /// {
        ///   "Processor": 
        ///   {
        ///     "TemperatureA": 42,
        ///     "TemperatureB": 23
        ///   }
        /// }
        /// 
        /// The following output is created:
        /// 
        /// Processor.TemperatureA = 42
        /// Processor.TemperatureB = 23
        /// </summary>
        /// <param name="json">The original json string.</param>
        /// <param name="separator">The separator used for combining names of different levels.</param>
        /// <param name="nameOnly">A value indicating whether only the name, without the hierarchy should be returned.</param>
        /// <returns>The json as a flattened dicrtionary.</returns>
        public static Dictionary<string, object?> Flatten(string json, string separator, bool nameOnly)
        {
            var root = JObject.Parse(json);
            var result = new Dictionary<string, object?>();
            FlattenToken(root, result, prefix: "", separator, nameOnly);
            return result;
        }

        /// <summary>
        /// Recursively flattens a token as returned from <see cref="JObject.Parse(string)"/>.
        /// </summary>
        /// <param name="token">The token to be parsed.</param>
        /// <param name="result">The already produced result.</param>
        /// <param name="prefix">The current prefix.</param>
        /// <param name="separator">The separator used for combining names of different levels.</param>
        /// <param name="nameOnly">A value indicating whether only the name, without the hierarchy should be returned.</param>
        private static void FlattenToken(JToken token, Dictionary<string, object?> result, string prefix, string separator, bool nameOnly)
        {
            switch (token.Type)
            {
                case JTokenType.Object:
                    foreach (var prop in token.Children<JProperty>())
                    {
                        var childPrefix = string.IsNullOrEmpty(prefix)
                            ? prop.Name
                            : (nameOnly ? prop.Name : $"{prefix}{separator}{prop.Name}");

                        FlattenToken(prop.Value, result, childPrefix, separator, nameOnly);
                    }
                    break;

                case JTokenType.Array:
                    int index = 0;
                    foreach (var item in token.Children())
                    {
                        FlattenToken(item, result, $"{prefix}{separator}{index}", separator, nameOnly);
                        index++;
                    }
                    break;

                default:
                    // Primitive value → store it
                    result[prefix] = ((JValue)token).Value;
                    break;
            }
        }
    }
}
