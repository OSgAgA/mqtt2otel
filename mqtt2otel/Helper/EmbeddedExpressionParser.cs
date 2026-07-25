using mqtt2otel.Interfaces;
using mqtt2otel.Parser;
using MQTTnet.Extensions.ManagedClient;
using Parlot.Fluent;
using System;
using System.Collections.Generic;
using System.Text;

namespace mqtt2otel.Helper
{
    /// <summary>
    /// A helper class for parsing embedded expressions in strings.
    /// </summary>
    public static class EmbeddedExpressionParser
    {
        private static IPayloadParser parser = new PayloadParser();

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
        /// <param name="variables">The variables that should be applied.</param>
        /// <param name="payload">The current payload.</param>
        /// <param name="topic">The current topic.</param>
        /// <returns>The expanded text.</returns>
        public static string Expand(string text, ParsingContext context, IPayloadParser? parser = null)
        {
            if (parser != null)
            {
                return Evaluate(text, context, parser);
            }
            else
            {
                return Evaluate(text, context, EmbeddedExpressionParser.parser);
            }
        }

        /// <summary>
        /// Expands all variables in source with the given replacements.
        /// </summary>
        /// <param name="source">A list of varialbes that should be expanded.</param>
        /// <param name="replacements">The replacements that will be used for expanding the source variables.</param>
        /// <param name="payload">The current payload.</param>
        /// <param name="topic">The current topic.</param>
        /// <returns>A new enumerable of expanded variables.</returns>
        public static IEnumerable<Variable> Expand(IEnumerable<Variable> source, IEnumerable<Variable> replacements, string payload, string topic)
        {
            var result = new List<Variable>();
            var context = new ParsingContext(replacements, payload, topic);

            return source.Select(variable => new Variable() { 
                Key = EmbeddedExpressionParser.Expand(variable.Key, context),
                Value = EmbeddedExpressionParser.Expand(variable.Value.ToString() ?? string.Empty, context) 
            }).ToList();
        }

        /// <summary>
        /// Evaluates an embedded expression. 
        /// </summary>
        /// <example>text = "This is a test"                                                         => result = "This is a test"</example>
        /// <example>text = "This is a $test", context.Variables = [ {"test": "important test} ]     => result = "This is a important test"</example>
        /// <example>text = "The answer to life universe and everything is: $(84/2)"                 => result = "The answer to life universe and everything is: 42"</example>
        /// <example>text = "The current payload is: $(PAYLOAD())", conmtext.Payload = "MyPayload"   => result = "The current payload is: MyPayload"</example>
        /// <example>text = "My constant is $(CONST('Hello')"                                        => result = "My constant is Hello"</example>
        /// <example>text = "My constant is $(CONST('Hello \'world\'')"                              => result = "My constant is Hello 'world'"</example>
        /// <example>text = "My constant is $(CONST('Hello \\world\\')"                              => result = "My constant is Hello \world\"</example>
        /// <param name="text">The text to be parsed.</param>
        /// <param name="context">The parsing context used by the payload parser for further processing embedded expressions.</param>
        /// <param name="parser">A payload parser used for evaluating embedded expressions.</param>
        /// <returns>The evaluated expression as a string.</returns>
        private static string Evaluate(string text, ParsingContext context, IPayloadParser parser)
        {
            var state = EmbeddedExpressionState.Text;
            var variableDict = new Dictionary<string, object>();

            foreach (var variable in context.Variables)
            {
                variableDict[variable.Key] = variable.Value;
            }

            var result = new StringBuilder();
            var variableName = new StringBuilder();
            var expression = new StringBuilder();
            var plainString = new StringBuilder();
            int bracketCount = 0;

            for (int i = 0; i < text.Length; i++)
            {
                char current = text[i];

                switch (state)
                {
                    case EmbeddedExpressionState.Text:
                        if (current == '$')
                        {
                            state = EmbeddedExpressionState.ExpressionStart;
                        }
                        else
                        {
                            result.Append(current);
                        }
                        break;
                    case EmbeddedExpressionState.ExpressionStart:
                        if (current == '(')
                        {
                            state = EmbeddedExpressionState.Expression;
                            bracketCount = 1;
                            expression = new StringBuilder();
                        }
                        else
                        {
                            state = EmbeddedExpressionState.VariableName;
                            variableName = new StringBuilder();
                            variableName.Append(current);
                        }
                        break;
                    case EmbeddedExpressionState.VariableName:
                        if (current == ' ')
                        {
                            state = EmbeddedExpressionState.Text;
                            string name = variableName.ToString();
                            if (variableDict.ContainsKey(name))
                            {
                                result.Append(variableDict[name]);
                            }
                        }
                        else
                        {
                            variableName.Append(current);
                        }
                        break;
                    case EmbeddedExpressionState.Expression:
                        if (current == '\'')
                        {
                            state = EmbeddedExpressionState.StringInsideEmbeddedExpression;
                            expression.Append('\'');
                        }
                        else if (current == ')')
                        {
                            bracketCount--;
                            if (bracketCount == 0)
                            {
                                state = EmbeddedExpressionState.Text;
                                string evaluatedExpression = parser.Parse<string>(string.Empty, expression.ToString(), context).Result;
                                result.Append(evaluatedExpression);
                            }
                            else
                            {
                                expression.Append(')');
                            }
                        }
                        else if (current == '(')
                        {
                            bracketCount++;
                            expression.Append('(');
                        }
                        else
                        {
                            expression.Append(current);
                        }
                        break;
                    case EmbeddedExpressionState.StringInsideEmbeddedExpression:
                        if (current == '\'')
                        {
                            state = EmbeddedExpressionState.Expression;
                            plainString.Append('\'');
                            expression.Append(plainString);
                        }
                        else if (current == '\\')
                        {
                            state = EmbeddedExpressionState.EscapeChar;
                        }
                        else
                        {
                            plainString.Append(current);
                        }
                        break;
                    case EmbeddedExpressionState.EscapeChar:
                        plainString.Append(current);
                        state = EmbeddedExpressionState.StringInsideEmbeddedExpression;
                        break;
                    default:
                        break;
                }
            }

            switch (state)
            {
                case EmbeddedExpressionState.Text:
                    break;
                case EmbeddedExpressionState.ExpressionStart:
                    result.Append("$");
                    break;
                case EmbeddedExpressionState.VariableName:
                    string name = variableName.ToString();
                    if (variableDict.ContainsKey(name))
                    {
                        result.Append(variableDict[name]);
                    }
                    break;
                case EmbeddedExpressionState.Expression:
                    result.Append("Missing ')' while parsing an embedded expression.");
                    break;
                case EmbeddedExpressionState.StringInsideEmbeddedExpression:
                    result.Append("Missing '\'' while parsing a string inside an expression.");
                    break;
                case EmbeddedExpressionState.EscapeChar:
                    result.Append("Missing '\'' while parsing a string inside an expression.");
                    break;
                default:
                    break;
            }

            return result.ToString();
        }

        /// <summary>
        /// Represents the available expression states for the finite state machine used for parsing an embedded expression.
        /// </summary>
        private enum EmbeddedExpressionState
        {
            /// <summary>
            /// Parsing a plain text.
            /// </summary>
            Text,

            /// <summary>
            /// The start of an expression, either a varialbe or an embedded expression is detected.
            /// </summary>
            ExpressionStart,

            /// <summary>
            /// Parsing a variable name.
            /// </summary>
            VariableName,

            /// <summary>
            /// Parsing an expression. 
            /// </summary>
            Expression,

            /// <summary>
            /// Parsing a string inside an embedded expression.
            /// </summary>
            StringInsideEmbeddedExpression,

            /// <summary>
            /// Parsing an escaped character inside a string.
            /// </summary>
            EscapeChar
        }
    }
}
