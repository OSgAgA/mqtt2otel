using mqtt2otel.Manifest;
using mqtt2otel.Shared.Helper;
using System.Text.Json;

namespace mqtt2otel
{
    /// <summary>
    /// Represents an open telemetry attribute..
    /// </summary>

    public class OtelAttribute
    {
        /// <summary>
        /// Tests whehter a name represents n internal attribute.
        /// </summary>
        /// <param name="name">The name under test.</param>
        /// <returns>A value indicating, whether the provided name represents an internal attribute.</returns>
        public static bool TestForInternalAttributeName(string name)
        {
            return name.StartsWith(InternalAttributes.InternalAttributePrefix);
        }

        /// <summary>
        /// Gets or sets the key under which the variable can be identified.
        /// 
        /// A key that starts with the <see cref="InternalAttributePrefix"/> will be escaped by adding a 
        /// preceeding underscore.
        /// </summary>
        public string Key { get; set; } = string.Empty;

        /// <summary>
        /// The backing field of the Value property.
        /// </summary>
        private object value;

        /// <summary>
        /// Gets or sets the value of the variable. 
        /// </summary>
        public object Value
        {
            get => this.value;

            set
            {
                if (value is JsonElement element)
                {
                    this.value = this.ToObject(element);
                }
                else
                {
                    this.value = value;
                }
            }
        }

        /// <summary>
        /// Ensures, that this attribute is not an internal attribute.
        /// </summary>
        public void EnsureNotInternal()
        {
            this.IsInternal = false;

            if (this.Key.StartsWith(InternalAttributes.InternalAttributePrefix))
            {
                this.Key = "_" + this.Key;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this attribute is for internal use. These attributes exist
        /// for technical reasons only and will be removed from the final result.
        /// </summary>
        public bool IsInternal { get; set; } = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="OtelAttribute"/> class.
        /// 
        /// This constructor should only be used for deserialization of attribute values. 
        /// </summary>
        public OtelAttribute()
        {
            this.Key = string.Empty;
            this.value = string.Empty;
        }


        /// <summary>
        /// Initializes a new instance of the <see cref="OtelAttribute"/> class.
        /// </summary>
        /// <param name="key">The attribute key.</param>
        /// <param name="value">The attribute value.</param>
        public OtelAttribute(string key, object value)
        {
            this.Key = key;
            this.value = value;
        }

        /// <summary>
        /// Creates an internal attribute
        /// </summary>
        /// <param name="key">The key of the internal attribute.</param>
        /// <param name="value">The value of the attribute as a string.</param>
        /// <returns>The created attribute.</returns>
        public static OtelAttribute CreateInternalAttribute(string key, string value)
        {
            var attribute = new OtelAttribute(string.Empty, value);
            attribute.Key = key;
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

        /// <summary>
        /// Converts the provided <see cref="JsonElement"/> into an object.
        /// </summary>
        /// <param name="element">The element to be converted.</param>
        /// <returns>The converted element.</returns>
        /// <exception cref="NotSupportedException">Thrown if an unsupported value type is used.</exception>
        private object ToObject(JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    var dict = new Dictionary<string, object?>();
                    foreach (var prop in element.EnumerateObject())
                        dict[prop.Name] = this.ToObject(prop.Value);
                    return dict;

                case JsonValueKind.Array:
                    var list = new List<object?>();
                    foreach (var item in element.EnumerateArray())
                        list.Add(this.ToObject(item));
                    return list;

                case JsonValueKind.String:
                    // Optionally detect dates first
                    if (element.TryGetDateTimeOffset(out var dto))
                        return dto;
                    return element.GetString() ?? string.Empty;

                case JsonValueKind.Number:
                    if (element.TryGetInt32(out int i)) return i;
                    if (element.TryGetInt64(out long l)) return l;
                    if (element.TryGetDecimal(out decimal d)) return d;
                    return element.GetDouble();

                case JsonValueKind.True:
                    return true;

                case JsonValueKind.False:
                    return false;

                case JsonValueKind.Null:
                case JsonValueKind.Undefined:

                default:
                    throw new NotSupportedException($"Unsupported ValueKind: {element.ValueKind}");
            }
        }
    }
}
