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
    public class SubscriptionStoreData(MqttSubscription subscription, Processor processor)
    {
        /// <summary>
        /// Gets the internal subscription associated with the mqtt subscription.
        /// </summary>
        public MqttSubscription Subscription { get; private set; } = subscription;

        /// <summary>
        /// Gets the processor associated with the mqtt subscription.
        /// </summary>
        public Processor Processor { get; private set; } = processor;
    }
}
