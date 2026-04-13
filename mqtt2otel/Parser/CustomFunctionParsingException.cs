using System;
using System.Collections.Generic;
using System.Text;

namespace mqtt2otel.Parser
{
    /// <summary>
    /// Represents an exception thrown if the system is not able to parse a string to an expected type.
    /// </summary>
    public class CustomFunctionParsingException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CustomFunctionParsingException"/> class.
        /// </summary>
        /// <param name="message">The message to further describe the issue.</param>
        public CustomFunctionParsingException(string message) : base(message) { }
    }
}
