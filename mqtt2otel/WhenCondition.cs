using mqtt2otel.Manifest;
using System.IO.Enumeration;

namespace mqtt2otel
{
    /// <summary>
    /// Represents a condition inside a <see cref="ConditionalAction"/>.
    /// </summary>
    public class WhenCondition
    {
        /// <summary>
        /// Gets or sets the pattern that the name should match to fullfill the condition. Set to null to accept any value.
        /// </summary>
        public string? Name { get; set; } = null;

        /// <summary>
        /// Gets or sets the signal data type that should match to fullfill the condition. Set to null to accept any value.
        /// </summary>
        public SignalDataType? SignalDataType { get; set; } = null;

        /// <summary>
        /// Gets or sets a value indicating whether the name matching pattern should ignore the case.
        /// </summary>
        public bool IgnoreCase { get; set; } = false;

        /// <summary>
        /// Checks if the condition applies to the provided values. All conditions must be fullfilled to match the rule.
        /// </summary>
        /// <param name="name">The name of the signal.</param>
        /// <param name="signalDataType">The signal data type.</param>
        /// <returns></returns>
        public bool Check(string name, SignalDataType signalDataType)
        {
            bool result = true;

            if (this.Name != null)
            {
                result &= FileSystemName.MatchesSimpleExpression(this.Name, name, this.IgnoreCase);
            }

            if (this.SignalDataType != null)
            {
                result &= (signalDataType == this.SignalDataType);
            }

            return result;
        }
    }
}
