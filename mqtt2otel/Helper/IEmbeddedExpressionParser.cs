using mqtt2otel.Interfaces;
using mqtt2otel.Parser;

namespace mqtt2otel.Helper
{
    /// <summary>
    /// A helper class for parsing embedded expressions in strings.
    /// </summary>
    public interface IEmbeddedExpressionParser
    {
        /// <summary>
        /// Expands all variables in source with the given replacements.
        /// </summary>
        /// <param name="attributes">A list of attributes that should be expanded.</param>
        /// <param name="replacements">The replacements that will be used for expanding the source variables.</param>
        /// <param name="message">The received message.</param>
        /// <returns>A new enumerable of expanded attributes.</returns>
        IEnumerable<OtelAttribute> Expand(IEnumerable<OtelAttribute> attributes, IEnumerable<Variable> replacements, MqttMessage message);

        /// <summary>
        /// Expand all embedded expressions that are found in a string.
        /// </summary>
        /// <example>text = "This is a test"                                                         => result = "This is a test"</example>
        /// <example>text = "This is a $test", context.Variables = [ {"test": "important test} ]     => result = "This is a important test"</example>
        /// <example>text = "The answer to life universe and everything is: $(84/2)"                 => result = "The answer to life universe and everything is: 42"</example>
        /// <example>text = "The current payload is: $(PAYLOAD())", conmtext.Payload = "MyPayload"   => result = "The current payload is: MyPayload"</example>
        /// <example>text = "My constant is $(CONST('Hello')"                                        => result = "My constant is Hello"</example>
        /// <example>text = "My constant is $(CONST('Hello \'world\'')"                              => result = "My constant is Hello 'world'"</example>
        /// <example>text = "My constant is $(CONST('Hello \\world\\')"                              => result = "My constant is Hello \world\"</example>
        /// <param name="text">The text that will be expanded.</param>
        /// <param name="context">The parsing context.</param>
        /// <returns>The expanded text.</returns>
        string Expand(string text, ParsingContext context);
    }
}