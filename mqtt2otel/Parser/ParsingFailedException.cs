using System;
using System.Collections.Generic;
using System.Text;

namespace mqtt2otel.Parser
{
    /// <summary>
    /// Represents an exception that will be thrown if a value cuold not be parsed from a string.
    /// 
    /// This class makes it easy to unit test certain failures without any dependency to a concrete
    /// exception message.
    /// </summary>
    public class ParsingFailedException : CustomFunctionParsingException
    {
        /// <summary>
        /// Gets the expected target type.
        /// </summary>
        public Type ExpectedTargetType { get; private set; }

        /// <summary>
        /// Gets the actual provided argument.
        /// </summary>
        public string ActualArgument { get; private set; }

        /// <summary>
        /// Gets the function name.
        /// </summary>
        public string FunctionName { get; private set; }

        /// <summary>
        /// Gets the zero-based index of the argument that could not be parsed.
        /// </summary>
        public int ArgumentIndex { get; private set; }

        /// <summary>
        /// Implements a new instance of the <see cref="ParsingFailedException"/> class.
        /// </summary>
        /// <param name="functionName">The name of the function that creates the exception.</param>
        /// <param name="argumentIndex">The zero based index of the argument that could not be parsed.</param>
        /// <param name="expectedArgumentType">The expected argument type.</param>
        /// <param name="actualArgument">The provided string argument, that could not be parsed.</param>
        public ParsingFailedException(string functionName, int argumentIndex, Type expectedArgumentType, string actualArgument)
            : base($"Can not parse argument {argumentIndex} (zero based) with value ({actualArgument}) of function {functionName} as type {expectedArgumentType.Name}")
        {
            this.FunctionName = functionName;
            this.ArgumentIndex = argumentIndex;
            this.ExpectedTargetType = expectedArgumentType;
            this.ActualArgument = actualArgument;
        }
    }
}
