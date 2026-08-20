using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace mqtt2otel.Parser
{
    /// <summary>
    /// Represents a <see cref="IParsingStrategy"/> that is able to parse a topic path.
    /// 
    /// The topic path pattern consists of two possible token:
    /// 
    ///   * A named segment token: MUST NOT start with a '[' AND MUST NOT end with a ']'
    ///   * A skip token: MUST start with a '[' AND MUST end with a ']'. The value between the brackets MUST be a positive integer.
    ///   
    /// A named token matches the first segment after the first occurance of the provided segment name and the skip token
    /// skips the provided amount of segments.
    /// 
    /// An empty string is returned, if the pattern does not match the topic.
    /// </summary>
    /// <example>Topic: "this/is/a/test", Pattern: "[0]"          => Result: "this"</example>
    /// <example>Topic: "this/is/a/test", Pattern: "[1]"          => Result: "is"</example>
    /// <example>Topic: "this/is/a/test", Pattern: "[4]"          => Result: ""</example>
    /// <example>Topic: "this/is/a/test", Pattern: "a"            => Result: "test"</example>
    /// <example>Topic: "this/is/a/test", Pattern: "a/"           => Result: "test"</example>
    /// <example>Topic: "this/is/a/test", Pattern: "nonExisting/" => Result: ""</example>
    /// <example>Topic: "this/is/a/test", Pattern: "is/"          => Result: "a"</example>
    /// <example>Topic: "this/is/a/test", Pattern: "is/[0]"       => Result: "a"</example>
    /// <example>Topic: "this/is/a/test", Pattern: "is/[1]"       => Result: "test"</example>
    /// <example>Topic: "this/is/is/test", Pattern: "is"          => Result: "is"</example>
    /// <example>Topic: "this/is/is/test", Pattern: "[2]/is"      => Result: "test"</example>
    /// <example>Topic: "this/is/is/test", Pattern: "is/is"       => Result: "test"</example>
    public class TopicPathStrategy : IParsingStrategy
    {
        /// <summary>
        /// Caches all parsed patterns for reuse.
        /// </summary>
        private Dictionary<string, List<TopicPathToken>> patternCache = new();

        /// <summary>
        /// The function name used by the strategy.
        /// </summary>
        public string Key => "TOPICPATH";

        /// <summary>
        /// Parses the topic by applying the provided pattern.
        /// </summary>
        /// <typeparam name="T">The expected return type: Must be string.</typeparam>
        /// <param name="pattern">The pattern to be applied. <see cref="TopicPathStrategy"/></param>
        /// <param name="context">The parsing context.</param>
        /// <returns>The interpreted topic, or an empty string, if pattern could not be applied.</returns>
        /// <exception cref="ArgumentException">When return type is not a string.</exception>
        public T Parse<T>(string pattern, ParsingContext context)
        {
            if (typeof(T) != typeof(string)) throw new ArgumentException("TopicPath parser must return a string.");

            if (!this.patternCache.ContainsKey(pattern))
            {
                this.patternCache[pattern] = this.ParsePattern(pattern);
            }

            return (T)(object)this.Parse(context.Message.Topic, this.patternCache[pattern]); ;
        }

        /// <summary>
        /// Parses the topic by applying the provided pattern.
        /// </summary>
        /// <typeparam name="T">The expected return type: Must be string.</typeparam>
        /// <param name="topic">The topic to be parsed.</param>
        /// <param name="pattern">The pattern to be applied. <see cref="TopicPathStrategy"/></param>
        /// <returns>The interpreted topic, or an empty string, if pattern could not be applied.</returns>
        private string Parse(string topic, List<TopicPathToken> pattern)
        {
            var segments = topic.Split('/');
            int topicPosition = 0;
            int patternPosition = 0;

            while (topicPosition < segments.Length && patternPosition < pattern.Count)
            {
                var token = pattern[patternPosition];
                var segment = segments[topicPosition];

                if (token.IsPathSegment)
                {
                    if (token.PathSegment == segment)
                    {
                        topicPosition++;
                        patternPosition++;
                    }
                    else
                    {
                        topicPosition++;
                    }
                }
                else
                {
                    topicPosition += token.SkipCount;
                    patternPosition++;
                }
            }

            if (topicPosition < segments.Length) return segments[topicPosition];

            return string.Empty;
        }

        /// <summary>
        /// Parses a pattern to a list of token.
        /// </summary>
        /// <param name="pattern">The pattern to be parsed. See  <see cref="TopicPathStrategy"/>.</param>
        /// <returns>The parsed pattern.</returns>
        private List<TopicPathToken> ParsePattern(string pattern)
        {
            List<TopicPathToken> result = new();

            var segments = pattern.Split('/');

            foreach (var segment in segments)
            {
                if (segment.StartsWith('[') && segment.EndsWith("]"))
                {
                    bool success = int.TryParse(segment[1..^1], out int intResult);
                    if (success)
                    {
                        if (intResult < 0) intResult = 0;

                        result.Add(new TopicPathToken(intResult));
                    }
                    else
                    {
                        result.Add(new TopicPathToken(segment));
                    }
                }
                else if (!string.IsNullOrWhiteSpace(segment))
                {
                    result.Add(new TopicPathToken(segment));
                }
            }

            return result;
        }
    }
}
