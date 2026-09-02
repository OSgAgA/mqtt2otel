using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace mqtt2otel.Manifest
{
    /// <summary>
    /// Represents a value with a given type.
    /// </summary>
    /// <param name="value">The value.of the object.</param>
    /// <param name="type">he data type of the value.</param>
    public class TypedValue(object? value, SignalDataType type)
    {
        /// <summary>
        /// The value.of the object.
        /// </summary>
        public object? Value = value;

        /// <summary>
        /// The data type of the value.
        /// </summary>
        public SignalDataType Type = type;
    }
}
