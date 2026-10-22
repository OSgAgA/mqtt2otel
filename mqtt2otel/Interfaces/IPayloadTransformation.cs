using mqtt2otel.Parser;

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
        /// <param name="expression">The expression that should be used for transforming the payload.</param>
        /// <param name="context">The execution context in which the strategy will be exeucted.</param>
        /// <returns>The transformed payload</returns>
        string Apply(string expression, ParsingContext context);
    }
}