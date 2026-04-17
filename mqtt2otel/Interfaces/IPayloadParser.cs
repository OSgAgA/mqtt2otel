using mqtt2otel.Parser;

namespace mqtt2otel.Interfaces
{
    /// <summary>
    /// Represents a parser for parsing string payloads, e.g. provided by mqtt subscriptions.
    /// </summary>
    public interface IPayloadParser
    {
        /// <summary>
        /// Adds a new strategy that can be used to parse a given payload to a certain type. The strategy will be 
        /// identified via its provided Key property.
        /// </summary>
        /// <param name="strategy">The strategy to be added.</param>
        void AddStrategy(IParsingStrategy strategy);

        /// <summary>
        /// Parses a string payload to a given type.
        /// </summary>
        /// <typeparam name="T">The expected result type.</typeparam>
        /// <param name="name">An identifier identifying the context of the parser. This enables the user to find the cause of the error.</param>
        /// <param name="payload">The payload to be parsed.</param>
        /// <param name="expression">The expression that should be applied to the payload.</param>
        /// <returns>The parsed value.</returns>
        Task<T> Parse<T>(string name, string payload, string expression);
    }
}