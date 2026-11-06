using mqtt2otel.Manifest;
using mqtt2otel.Shared.Helper;

namespace mqtt2otel
{
    /// <summary>
    /// Represents an open telemetry attribute..
    /// </summary>

    public class OtelAttribute
    {
        /// <summary>
        /// The backing field of the <see cref="Key"/> property.
        /// </summary>
        private string key = string.Empty;

        /// <summary>
        /// Gets or sets the key under which the variable can be identified.
        /// 
        /// A key that starts with the <see cref="InternalAttributePrefix"/> will be escaped by adding a 
        /// preceeding underscore.
        /// </summary>
        public string Key
        {
            get
            {
                return key;
            }
            set
            {
                if (value.StartsWith(InternalAttributes.InternalAttributePrefix))
                {
                    key = "_" + InternalAttributes.InternalAttributePrefix;
                }
                else
                {
                    key = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets the value of the variable. 
        /// </summary>
        public object Value { get; init; }

        /// <summary>
        /// Gets a value indicating whether this attribute is for internal use. These attributes exist
        /// for technical reasons only and will be removed from the final result.
        /// </summary>
        public bool IsInternal { get; private set; } = false;

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
        /// Creates an internal attribute
        /// </summary>
        /// <param name="key">The key of the internal attribute.</param>
        /// <param name="value">The value of the attribute as a string.</param>
        /// <returns>The created attribute.</returns>
        internal static OtelAttribute CreateInternalAttribute(string key, string value)
        {
            var attribute = new OtelAttribute(string.Empty, value);
            attribute.key = key;
            attribute.IsInternal = true;

            return attribute;
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

        /// <summary>
        /// Clones the attribute.
        /// </summary>
        /// <returns>The created clone.</returns>
        public OtelAttribute Clone()
        {
            return (OtelAttribute)this.MemberwiseClone();
        }
    }
}
