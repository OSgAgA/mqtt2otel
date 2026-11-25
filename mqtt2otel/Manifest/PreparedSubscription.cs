using System;
using System.Collections.Generic;
using System.Text;

namespace mqtt2otel.Manifest
{
    /// <summary>
    /// Represents a subscription that stores messages until picked up by a dynamic subscription.
    /// 
    /// This is needed for response subscriptions, as it is possible that the response arrives, before
    /// the system could subscribe to the response topic.
    /// </summary>
    public class PreparedSubscription : NamedIdObject
    {
        /// <summary>
        /// Gets or sets a value indicating whether the subscription is active.
        /// </summary>
        public bool IsActive { get; set; } = false;

        /// <summary>
        /// Gets or sets the retention time of messages in seconds. If a message is not processed after that
        /// timespan, it will be deleted.
        /// </summary>
        public int TimeoutInS { get; set; } = 5;

        /// <summary>
        /// Gets or sets the maximum message count before messages are thrown away.
        /// </summary>
        public int MaxMessageCount { get; set; } = 100;
    }
}
