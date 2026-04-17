namespace mqtt2otel.Interfaces
{
    /// <summary>
    /// Represents a class that can transform a string payload to another string payload based on the provided
    /// <see cref="ITransformationStrategy"/> strategies.
    /// </summary>
    public interface IPayloadTransformation
    {
        /// <summary>
        /// Applies a transformation to a payload.
        /// </summary>
        /// <param name="category">The subscription type that triggered the transformation.</param>
        /// <param name="name">An identifier to help the user to idnetify the correct position of the transformation in case of an error.</param>
        /// <param name="payload">The payload that should be processed.</param>
        /// <param name="expression">The expression that should be used for transforming the payload.</param>
        /// <returns>The transformed payload</returns>
        Task<string> Apply(string name, string payload, string expression);
    }
}