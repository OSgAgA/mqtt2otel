using System;
using System.Collections.Generic;
using System.Text;

namespace mqtt2otel.Parser
{
    /// <summary>
    /// Represents an excpetion called when the type of an argument provided to a function doesn't
    /// meet the functions expectation.
    /// 
    /// This class makes it easy to unit test certain failures without any dependency to a concrete
    /// exception message.
    /// </summary>
    public class ArgumentTypeException : CustomFunctionParsingException
    {
        /// <summary>
        /// Gets or sets the expected argument type.
        /// </summary>
        public Type ExpectedArgumentType { get; private set; }

        /// <summary>
        /// Gets or sets the actual argument as a string representation.
        /// </summary>
        public string ActualArgument { get; private set; }

        /// <summary>
        /// Gets or sets the function for which the argument has been provided.
        /// </summary>
        public string FunctionName { get; private set; }

        /// <summary>
        /// Gets or sets the zero based argument index.
        /// </summary>
        public int ArgumentIndex { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ArgumentTypeException"/> class.
        /// </summary>
        /// <param name="functionName">The name of the function to which the argument was provided.</param>
        /// <param name="argumentIndex">The zero based index of the argument.</param>
        /// <param name="expectedArgumentType">The type expected by the function.</param>
        /// <param name="actualArgument">The provided argument as a string representation.</param>
        public ArgumentTypeException(string functionName, int argumentIndex, Type expectedArgumentType, string actualArgument)
            : base($"Invalid argument type for argument {argumentIndex} (zero based) of function {functionName}. Type {expectedArgumentType.Name} was expected, but {actualArgument} has been received.")
        {
            this.FunctionName = functionName;
            this.ArgumentIndex = argumentIndex;
            this.ExpectedArgumentType = expectedArgumentType;
            this.ActualArgument = actualArgument;
        }
    }
}
