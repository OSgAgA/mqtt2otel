using mqtt2otel.Manifest;
using System;
using System.Collections.Generic;
using System.Text;

namespace mqtt2otel
{
    /// <summary>
    /// Represents an open telemetry attribute..
    /// </summary>

    public class OtelAttribute
    {
        /// <summary>
        /// Gets or sets the key under which teh variable can be identified.
        /// </summary>
        public string Key { get; init; }

        /// <summary>
        /// Gets or sets the value of the variable. 
        /// </summary>
        public object Value { get; init; }

        /// <summary>
        /// Initializes a new instance of the <see cref="OtelAttribute"/> class.
        /// 
        /// This constructor should only be used for deserialization of attribute values. 
        /// </summary>
        public OtelAttribute()
        {
            this.Key = string.Empty;
            this.Value = string.Empty;
        }


        /// <summary>
        /// Initializes a new instance of the <see cref="OtelAttribute"/> class.
        /// </summary>
        /// <param name="key">The attribute key.</param>
        /// <param name="value">The attribute value.</param>
        public OtelAttribute(string key, object value)
        {
            this.Key = key;
            this.Value = value;
        }


        /// <summary>
        /// Validates the attribute..
        /// </summary>
        /// <param name="context">The context for providing error messages.</param>
        /// <param name="result">The validation result object.</param>
        public void Validate(string context, ValidationResult result)
        {
            if (string.IsNullOrWhiteSpace(this.Key)) result.AddError($"{context}: An attribute with an empty Key found. Please set the key to a non empty value.");
        }
    }
}
