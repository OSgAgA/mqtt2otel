using GrokNet;
using mqtt2otel.Interfaces;
using mqtt2otel.Parser;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using Dissect.Extended.Net.Library;

namespace mqtt2otel.Transformation
{
    /// <summary>
    /// Represents a <see cref="ITransformationStrategy"/> that is able to parse grok patterns from string payloads.
    /// </summary>
    public class DissectStrategy : ITransformationStrategy
    {
        /// <summary>
        /// The function name used by the strategy.
        /// </summary>
        public string Key => "DISSECT";

        /// <summary>
        /// Applies the grok pattern to the given payload and returns a structured json.
        /// </summary>
        /// <param name="payload">The payload to be processed.</param>
        /// <param name="pattern">The grok pattern to be applied. See <see cref="https://www.elastic.co/docs/reference/logstash/plugins/plugins-filters-grok"/></param>
        /// <param name="context">The execution context in which the strategy will be exeucted.</param>
        /// <returns>The parsed payload as a structured json string.</returns>
        public string Apply(string payload, string pattern, ParsingContext context)
        {
            var parser = new DissectParser(pattern);

            var result = parser.Parse(payload);

            var resultAsDict = result.ToDictionary();
            resultAsDict["original_value"] = payload;

            var resultAsJson = JsonConvert.SerializeObject(resultAsDict);

            return resultAsJson;
        }
    }
}
