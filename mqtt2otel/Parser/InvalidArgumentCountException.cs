using System;
using System.Collections.Generic;
using System.Text;

namespace mqtt2otel.Parser
{
    /// <summary>
    /// Represents an exception thrown when a custom expression function will be provided with the wrong argument count.
    /// 
    /// This class makes it easy to unit test certain failures without any dependency to a concrete
    /// exception message.
    /// </summary>
    public class InvalidArgumentCountException : CustomFunctionParsingException
    {
        /// <summary>
        /// Gets the minimum amount of aruments that has been expected (inclusive).
        /// </summary>
        public int ExpectedArgumentCountMin { get; private set; }

        /// <summary>
        /// Gets the maximum amount of arguments that has been expected (inclusive).
        /// </summary>
        public int ExpectedArgumentCountMax { get; private set; }

        /// <summary>
        /// Gets the received number of arguments.
        /// </summary>
        public int ActualArgumentCount { get; private set; }

        /// <summary>
        /// Gets the name of the function for which the arguments have been provided.
        /// </summary>
        public string FunctionName { get; private set; }

        /// <summary>
        /// Creates a string that represents an argument range.
        /// </summary>
        /// <example>min = 1, max = 2 => 1-2</example>
        /// <example>min = 1, max = 1 => 1</example>
        /// <param name="min">The (inclusive) minimum.</param>
        /// <param name="max">The inclusive maximum.</param>
        /// <returns>A string reprentation for the provided range.</returns>
        private static string CreateArgumentRangeString(int min, int max)
        {
            if (min == max)
            {
                return min.ToString();
            }
            else
            {
                return $"{min}-{max}";
            }
        }

        /// <summary>
        /// Initilaizes a new instance of the <see cref="InvalidArgumentCountException"/> class.
        /// </summary>
        /// <param name="functionName">The name of the function.</param>
        /// <param name="expectedArgumentCountMin">The (inclusive) minimum of arguments that have been expected.</param>
        /// <param name="expectedArgumentCountMax">The (inclusive) maximum of arguments that have been expected.</param>
        /// <param name="actualArgumentCount">The actual amount of arguments that have been received.</param>
        public InvalidArgumentCountException(string functionName, int expectedArgumentCountMin, int expectedArgumentCountMax, int actualArgumentCount) 
            : base($"Invalid argument count in function {functionName}. {CreateArgumentRangeString(expectedArgumentCountMin, expectedArgumentCountMax)} arguments were expected, but {actualArgumentCount} arguments have been received.")
        {
            this.ExpectedArgumentCountMin = expectedArgumentCountMin;
            this.ExpectedArgumentCountMax = expectedArgumentCountMax;
            this.ActualArgumentCount = actualArgumentCount;
            this.FunctionName = functionName;
        }
    }
}
