using NCalc.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;

namespace mqtt2otel.Parser
{
    /// <summary>
    /// Represents an exception rasied when an expression cannot be parsed.
    /// 
    /// This is a wrappter class, that holds the originaly rasied exception as an inner exception.
    /// </summary>
    public class ExpressionParsingException : Exception
    {
        /// <summary>
        /// Formats the message string based on the provided data.
        /// </summary>
        /// <param name="ex">The inner exception that occured during parsing.</param>
        /// <param name="name">An identifier for the current context.</param>
        /// <param name="expression">The expression that could not be parsed.</param>
        /// <returns>The formatted message.</returns>
        private static string FormatMessage(Exception ex, string name, string expression)
        {
            string message = ex.Message;
            if (ex.InnerException != null) message = ex.InnerException.Message;

            return $"[{name}]: Error while parsing expression \"{expression}\": {message}";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExpressionParsingException"/> class.
        /// </summary>
        /// <param name="ex">The inner exception that occured during parsing.</param>
        /// <param name="name">An identifier for the current context.</param>
        /// <param name="expression">The expression that could not be parsed.</param>
        public ExpressionParsingException(Exception ex, string name, string expression) : base(FormatMessage(ex, name, expression), ex) 
        {
        }
    }
}