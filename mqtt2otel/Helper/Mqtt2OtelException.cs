using System;
using System.Collections.Generic;
using System.Text;

namespace mqtt2otel.Helper
{
    /// <summary>
    /// Defines the generic exception that is used inside the applicatin, when no, more specific, exceptions are available.
    /// </summary>
    public class Mqtt2OtelException : Exception
    {
        /// <summary>
        /// Initialize a new instance of the <see cref="Mqtt2OtelException"/> class.
        /// </summary>
        /// <param name="message">The exception message.</param>
        public Mqtt2OtelException(string message) : base(message) { }
    }
}
