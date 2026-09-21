using System;
using System.Collections.Generic;
using System.Text;

namespace mqtt2otel.Metadata
{
    /// <summary>
    /// Represents the data send from a mqtt broker.
    /// </summary>
    public class MqttSetupData
    {
        /// <summary>
        /// Gets or sets the example topic.
        /// </summary>
        public string Topic { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the example payload.
        /// </summary>
        public string Payload { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the example user properies.
        /// </summary>
        public List<UserProperty> UserProperties { get; set; } = new();
    }
}
