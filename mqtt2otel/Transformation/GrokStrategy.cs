using GrokNet;
using mqtt2otel.Parser;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace mqtt2otel.Transformation
{
    /// <summary>
    /// Represents a <see cref="ITransformationStrategy"/> that is able to parse grok patterns from string payloads.
    /// </summary>
    public class GrokStrategy : ITransformationStrategy
    {
        /// <summary>
        /// The function name used by the strategy.
        /// </summary>
        public string Key => "GROK";

        /// <summary>
        /// Applies the grok pattern to the given payload and returns a structured json.
        /// </summary>
        /// <param name="payload">The payload to be processed.</param>
        /// <param name="pattern">The grok pattern to be applied. See <see cref="https://www.elastic.co/docs/reference/logstash/plugins/plugins-filters-grok"/></param>
        /// <returns>The parsed payload as a structured json string.</returns>
        public string Apply(string payload, string pattern)
        {
            var grok = new Grok(pattern);

            var result = grok.Parse(payload);//.ToDictionary();

            var resultAsDict = this.CreateFlatDictionary(result);
            resultAsDict["original_value"] = payload;

            var resultAsJson = JsonConvert.SerializeObject(resultAsDict);

            return resultAsJson;
        }

        /// <summary>
        /// Creates a float dictionary from a grok result.
        /// </summary>
        /// <param name="grokResult">The source.</param>
        /// <returns>The created dictionary.</returns>
        private Dictionary<string, object?> CreateFlatDictionary(GrokResult grokResult)
        {
            var result = new Dictionary<string, object?>();

            foreach (var item in grokResult)
            {
                result[item.Key] = item.Value;
            }

            return result;
        }
    }
}
