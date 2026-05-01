using mqtt2otel.Manifest;
using System;
using System.Collections.Generic;
using System.Text;

namespace mqtt2otel.Stores
{
    /// <summary>
    /// Stores all subscriptions to mqtt brokers.
    /// </summary>
    public class SubscriptionStore
    {
        /// <summary>
        /// Stores data and associates it with a subscription topic. Multiple subscriptions can subscribe to the
        /// same topic.
        /// </summary>
        private Dictionary<string, List<SubscriptionStoreData>> store = new ();

        /// <summary>
        /// Stores a subscription.
        /// </summary>
        /// <param name="topic">The topic to identify the subscription.</param>
        /// <param name="subscription">The internal subscription.</param>
        /// <param name="processor">The internal processor.</param>
        public void Store(string topic, MqttSubscription subscription, Processor processor)
        {
            var data = new SubscriptionStoreData(subscription, processor);

            if (store.ContainsKey(topic))
            {
                store[topic].Add(data);
            }
            else
            {
                store[topic] = new List<SubscriptionStoreData>() { data };
            }
        }

        /// <summary>
        /// Retrieves data from the store.
        /// </summary>
        /// <param name="topic">The mqtt subscription topic.</param>
        /// <returns>A list of all subscriptions available for this topic.</returns>
        public IEnumerable<SubscriptionStoreData> Retrieve(string topic)
        {
            if (this.ContainsTopic(topic)) return store[topic];

            return new List<SubscriptionStoreData>();
        }

        /// <summary>
        /// Tests whether the store contains data for the provided topic.
        /// </summary>
        /// <param name="topic">The mqtt topic to check for.</param>
        /// <returns>True, if data is available for this topic, false otherwise.</returns>
        public bool ContainsTopic(string topic)
        {
            return store.ContainsKey(topic);
        }

        /// <summary>
        /// Removes all data from the store.
        /// </summary>
        public void Clear()
        {
            this.store.Clear();
        }
    }
}
