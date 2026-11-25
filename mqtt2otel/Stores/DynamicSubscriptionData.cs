using mqtt2otel.Manifest;
using System;
using System.Collections.Generic;
using System.Text;

namespace mqtt2otel.Stores
{
    /// <summary>
    /// Represents a subscription to a mqtt broker and connects it with the associated 
    /// mqtt2otel subscriptions and processors.
    /// </summary>
    /// <param name="subscription">The internal subscription associated with the mqtt subscription.</param>
    /// <param name="processor">The processor associated with the mqtt subscription.</param>
    /// <param name="timeoutInSeconds">Timeout in seconds</param>
    public class DynamicSubscriptionData(string topic, MqttSubscription? subscription = null, Processor? processor = null, int timeoutInSeconds = 2)
    {
        /// <summary>
        /// Gets or sets the topic of the subscription.
        /// </summary>
        public string Topic { get; set; } = topic;

        /// <summary>
        /// Gets the internal subscription associated with the mqtt subscription.
        /// </summary>
        public MqttSubscription? Subscription { get; set; } = subscription;

        /// <summary>
        /// Gets or sets the received messages, that are not yet processed.
        /// </summary>
        public List<MqttMessage> MessageBuffer { get; set; } = new List<MqttMessage>();

        /// <summary>
        /// Gets the processor associated with the mqtt subscription.
        /// </summary>
        public Processor? Processor { get; set; } = processor;

        /// <summary>
        /// Gets or sets the timeout after which the object will be discarded.
        /// </summary>
        public int TimeoutInSeconds { get; set; } = timeoutInSeconds;

        /// <summary>
        /// The date and time, when this object was created.
        /// </summary>
        public DateTime Created = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the subscription id, which is used for unsubscribing. If set to null, this is not subscribed
        /// to yet.
        /// </summary>
        public uint? SubscriptionId { get; set; } = null;

        /// <summary>
        /// Gets or sets the name of the otel connection used while subscribing.
        /// </summary>
        public string? ConnectionName { get; set; } = null;

        /// <summary>
        /// The subscription is active when neither the max count is reached nor the timeout has been exceeded.
        /// </summary>
        public bool CanBeDeleted
        {
            get => (DateTime.UtcNow - this.Created).TotalSeconds >= TimeoutInSeconds;
        }
    }
}
