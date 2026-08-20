using System;
using System.Collections.Generic;
using System.Text;

namespace mqtt2otel
{
    /// <summary>
    /// Represents a received mqtt message.
    /// </summary>
    public class MqttMessage(uint subscriptionId = 0, string topic = "", string payload = "", List<UserProperty>? userProperties = null)
    {
        /// <summary>
        /// Gets or sets the message payload.
        /// </summary>
        public string Payload { get; set; } = payload;

        /// <summary>
        /// Gets or sets the id of the mqtt broker subscription, that triggered the message delivery.
        /// </summary>
        public uint SubscriptionId { get; set; } = subscriptionId;

        /// <summary>
        /// Gets or sets the message topic.
        /// </summary>
        public string Topic { get; set; } = topic;

        /// <summary>
        /// Gets or sets the user properties associated with this message.
        /// </summary>
        public List<UserProperty> UserProperties { get; set; } = userProperties ?? new List<UserProperty>();
    }
}
