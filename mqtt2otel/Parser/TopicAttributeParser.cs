using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace mqtt2otel.Parser
{
    /// <summary>
    /// A static class for parsing mqtt topics to attributes using a simple, but easy to read syntay.
    /// 
    /// The syntax consists of a path separator character (Default: "/"), a wildcard character (Default: "#") and the any character (Default: "_"). 
    /// All characters between the start/end/path separator are identified as attribute keys for the part of the path matching the depth.
    /// 
    /// An attribute key cannot be empty or whitespace only. If this rule is violated, the pattern will not be further processed.
    /// 
    /// The wildcard matches any (including 0) further topic segments and must be the last segment of a pattern. If the wildcard is used inside a 
    /// pattern only the attributes, that matched until this segment will be returned.
    /// 
    /// If no result is found, the pattern is ignored. If the same attribute key is used multiple time it overwrites the previous one.
    /// </summary>
    /// <example> Topic: "logs/sensor/1234/temperature", Pattern: "_/_/Device/_" ==> Created attribute: Key: "Device", Value: "1234".</example>
    /// <example> Topic: "logs/sensor/1234/temperature", Pattern: "_/Device/Device/_" ==> Created attribute: Key: "Device", Value: "1234".</example>
    /// <example> Topic: "logs/sensor/1234", Pattern: "_/_/Device/_" ==> Created attributes: NONE>.</example>
    /// <example> Topic: "logs/sensor/1234/temperature", Pattern: "_/  /Device/_" ==> Created attributes: NONE>.</example>
    /// <example> Topic: "logs/sensor/1234/temperature/valueA", Pattern: "_/_/Device/%" ==> Created attribute: Key: "Device", Value: "1234".</example>
    /// <example> Topic: "logs/sensor/1234", Pattern: "_/_/Device/%" ==> Created attribute: Key: "Device", Value: "1234".</example>
    public static class TopicAttributeParser
    {
        /// <summary>
        /// Identifies the character used to split the provided path into segments.
        /// </summary>
        public static char PathSeparator = '/';

        /// <summary>
        /// Identifies the character that accepts any value for the visited segment.
        /// </summary>
        public static char AnyCharacter = '_';

        /// <summary>
        /// Identifies a wildcard segment inside a pattern. 
        /// </summary>
        public static string Wildcard = "%";

        /// <summary>
        /// Parses the provided topic by using the provided pattern. Details <see cref="TopicAttributeParser"/>.
        /// </summary>
        /// <param name="topic">The topic to be parsed.</param>
        /// <param name="pattern">The pattern that should be applied to the topic.</param>
        /// <returns>The generated attributes, or an empty list, if the pattern could not be matched.</returns>
        public static IEnumerable<OtelAttribute> Parse(string topic, string pattern)
        {
            var result = new List<OtelAttribute>();

            if (string.IsNullOrWhiteSpace(topic)) return result;

            var patternSegments = pattern.Split(PathSeparator);
            var topicSegments = topic.Split(PathSeparator);

            if (patternSegments.Length == 0 || topicSegments.Length == 0) return result;

            bool hasWildcard = patternSegments[^1] == Wildcard;
            int iterationMax = patternSegments.Length - (hasWildcard ? 1 : 0);

            if (iterationMax > topicSegments.Length) return result;

            if (topicSegments.Length >  iterationMax && !hasWildcard) return result;

            for (int i = 0; i< iterationMax; i++)
            {
                if (string.IsNullOrWhiteSpace(patternSegments[i])) return result;
                if (patternSegments[i].Length == 1 && patternSegments[i][0] == AnyCharacter) continue;
                if (patternSegments[i] == Wildcard) return result;

                result.Add(new OtelAttribute() { Key = patternSegments[i], Value = topicSegments[i] });
            }

            return result;
        }
    }
}
