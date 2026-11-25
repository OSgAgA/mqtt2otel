using mqtt2otel.Manifest;
using mqtt2otel.Stores;
using System;
using System.Collections.Generic;
using System.Text;

namespace mqtt2otel.Interfaces
{
    public interface IDynamicSubscriptionStore
    {
        /// <summary>
        /// Gets an exiting dynamic subscription or creates a new one if none exists.
        /// </summary>
        /// <param name="topic">The topic name for receiving the subscription.</param>
        /// <returns>The existing or newly created response subscription.</returns>
        public DynamicSubscriptionData GetOrCreateDynamicSubscriptionData(string topic);

        /// <summary>
        /// Stores or updates a dynamic subscription.
        /// 
        /// An existing entry will only update values that are null. All other values will not be changed.
        /// </summary>
        /// <param name="topic">The topic to identify the subscription.</param>
        /// <param name="subscription">The internal subscription.</param>
        /// <param name="processor">The internal processor.</param>
        public void UpsertDynamicSubscription(string topic, MqttSubscription subscription, Processor processor);

        /// <summary>
        /// Deletes all not needed subscriptions.
        /// </summary>
        public void CleanupDynamicSubscriptions();

        /// <summary>
        /// This event will be called, when a dynamic subscription has been removed from the store.
        /// </summary>
        public event EventHandler<DynamicSubscriptionData>? OnSubscriptionRemoved;

    }
}
