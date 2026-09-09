using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Xml.Linq;

namespace mqtt2otel
{
    /// <summary>
    /// Represents a Mqtt user property, that is available from mqtt V5.
    /// </summary>
    public class UserProperty
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UserProperty"/> type.
        /// </summary>
        public UserProperty() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="UserProperty"/> type.
        /// </summary>
        /// <param name="name">The user property name.</param>
        /// <param name="value">The user property value.</param>
        public UserProperty(string name, string value)
        {
            this.Name = name;
            this.Value = value;
        }

        /// <summary>
        /// Gets or sets the name of the user property.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the string value of the the user property.
        /// </summary>
        public string Value { get; set; } = string.Empty;


    }
}
