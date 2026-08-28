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
        /// <returns>The json as a flattened dicrtionary.</returns>
        public static Dictionary<string, object?> Flatten(string json)
        {
            var root = JObject.Parse(json);
            var result = new Dictionary<string, object?>();
            FlattenToken(root, result, prefix: "");
            return result;
        }

        /// <summary>
        /// Recursively flattens a token as returned from <see cref="JObject.Parse(string)"/>.
        /// </summary>
        /// <param name="token">The token to be parsed.</param>
        /// <param name="result">The already produced result.</param>
        /// <param name="prefix">The current prefix.</param>
        private static void FlattenToken(JToken token, Dictionary<string, object?> result, string prefix)
        {
            switch (token.Type)
            {
                case JTokenType.Object:
                    foreach (var prop in token.Children<JProperty>())
                    {
                        var childPrefix = string.IsNullOrEmpty(prefix)
                            ? prop.Name
                            : $"{prefix}.{prop.Name}";

                        FlattenToken(prop.Value, result, childPrefix);
                    }
                    break;

                case JTokenType.Array:
                    int index = 0;
                    foreach (var item in token.Children())
                    {
                        FlattenToken(item, result, $"{prefix}.{index}");
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
