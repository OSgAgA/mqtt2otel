using System;
using System.Collections.Generic;
using System.Text;

namespace mqtt2otel.Parser
{
    /// <summary>
    /// Represents the exception that will be thrown if the parser cannot find a function.
    /// </summary>
    /// <param name="functionName">The name of the function that was not found.</param>
    public class FunctionNotFoundException(string functionName) : Exception
    {
        /// <summary>
        /// Gets or sets the name of the function that was not found.
        /// </summary>
        public string FunctionName { get; set; } = functionName;

        /// <summary>
        /// Provides a human readable error message.
        /// </summary>
        /// <returns>The error message.</returns>
        public override string ToString()
        {
            return $"Function '{this.FunctionName}' not found.";
        }
    }
}
