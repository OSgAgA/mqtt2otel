using MQTTnet.Packets;
using System;
using System.Collections.Generic;
using System.Text;

namespace mqtt2otel.Helper
{
    /// <summary>
    /// Provides extension methods for <see cref="UserProperty"/> instances.
    /// </summary>
    public static class UserPropertiesExtensions
    {
        /// <summary>
        /// Converts a <see cref="MqttUserProperty"/> to a <see cref="UserProperty"/>.
        /// </summary>
        /// <param name="properties">A list of mqtt user properties.</param>
        /// <returns>The converted properties as a list.</returns>
        public static List<UserProperty> ToUserProperties(this List<MqttUserProperty> properties)
        {
            var result = new List<UserProperty>();

            if (properties == null) return result;

            foreach (MqttUserProperty property in properties)
            {
                result.Add(Convert(property));
            }

            return result;
        }

        /// <summary>
        /// Converts the given mqtt user properties to open telemetry attributes. Ignores empty name attributes. An attribute is empty, 
        /// if its name is null, empty, or only consists of whitespace.
        /// 
        /// </summary>
        /// <param name="properties">The source user properties.</param>
        /// <returns>The converted attributes.</returns>
        public static List<OtelAttribute> ToOtelAttributes(this List<UserProperty> properties)
        {
            var result = new List<OtelAttribute>();

            properties.ForEach(prop => result.Add(new OtelAttribute(prop.Name, prop.Value)));

            return result.Where(prop => !string.IsNullOrWhiteSpace(prop.Key)).ToList();
        }

        /// <summary>
        /// Converts a <see cref="MqttUserProperty"/> to a <see cref="UserProperty"/>.
        /// </summary>
        /// <param name="property">The property to be converted.</param>
        /// <returns>The converted property.</returns>
        public static UserProperty Convert(this MqttUserProperty property)
        {
            return new UserProperty(property.Name, property.ReadValueAsString());
        }

    }
}
