using mqtt2otel.Manifest;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace mqtt2otel
{
    /// <summary>
    /// Represents a string variable that can be used to either expand strings that are referring to this varialbe, or can
    /// be converted to open telemetry attributes.
    /// </summary>
    public class Variable
    {
        /// <summary>
        /// Gets or sets the key under which teh variable can be identified.
        /// </summary>
        public string Key { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the value of the variable. 
        /// </summary>
        public object Value { get; set; } = string.Empty;

        /// <summary>
        /// Validates the variable.
        /// </summary>
        /// <param name="context">The context for providing error messages.</param>
        /// <param name="result">The validation result object.</param>
        public void Validate(string context, ValidationResult result)
        {
            if (string.IsNullOrWhiteSpace(this.Key)) result.AddError($"{context}: A variable with an empty Key found. Please set the key to a non empty value.");
        }
    }
}
