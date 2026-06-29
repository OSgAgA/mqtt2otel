using mqtt2otel.Manifest;
using System;
using System.Collections.Generic;
using System.Reflection.Metadata.Ecma335;
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
        private Dictionary<uint, List<SubscriptionStoreData>> store = new();

        /// <summary>
        /// Maps all supported topic wildcards with their broker subscription ids.
        /// </summary>
        private Dictionary<string, uint> topicToBrokerSubscriptionIdMapping = new();

        /// <summary>
        /// Stores a subscription.
        /// </summary>
        /// <param name="brokerSubscriptionId">The id of the broker subscription that will trigger the event.</param>
        /// <param name="topic">The topic to identify the subscription.</param>
        /// <param name="subscription">The internal subscription.</param>
        /// <param name="processor">The internal processor.</param>
        public void Store(uint brokerSubscriptionId, MqttSubscription subscription, Processor processor)
        {
            if (subscription.Topic == null) return;

            var data = new SubscriptionStoreData(subscription, processor);

            this.store[brokerSubscriptionId] = new List<SubscriptionStoreData>() { data };
            this.topicToBrokerSubscriptionIdMapping[subscription.Topic] = brokerSubscriptionId;
        }

        /// <summary>
        /// Retrieves data from the store.
        /// </summary>
        /// <param name="brokerSubscriptionId">The id of the broker subscription that will trigger the event.</param>
        /// <param name="topic">The mqtt subscription topic.</param>
        /// <returns>A list of all subscriptions available for this topic.</returns>
        public IEnumerable<SubscriptionStoreData> Retrieve(uint brokerSubscriptionId)
        {
            if (this.ContainsSubscriptionId(brokerSubscriptionId)) return store[brokerSubscriptionId];

            return new List<SubscriptionStoreData>();
        }

        /// <summary>
        /// Tests whether the store contains data for the provided broker subscription id.
        /// </summary>
        /// <param name="topic">The mqtt topic to check for.</param>
        /// <returns>True, if data is available for this topic, false otherwise.</returns>
        public bool ContainsSubscriptionId(uint brokerSubscriptionId)
        {
            return store.ContainsKey(brokerSubscriptionId);
        }

        /// <summary>
        /// Removes all data from the store.
        /// </summary>
        public void Clear()
        {
            this.store.Clear();
            this.topicToBrokerSubscriptionIdMapping.Clear();
        }

        /// <summary>
        /// Tests if the same broker subsrciption already exists and if yes, adds the data to the existing subscription.
        /// </summary>
        /// <param name="topic">The topic. May include wildcards.</param>
        /// <param name="subscription">The internal subscription.</param>
        /// <param name="processor">The internal processor.</param>
        /// <returns>True if data has been added to an existing subscription or false otherwise.</returns>
        public bool AddToExistingSubscription(string topic, MqttSubscription subscription, Processor processor)
        {
            if (this.topicToBrokerSubscriptionIdMapping.ContainsKey(topic))
            {
                uint id = this.topicToBrokerSubscriptionIdMapping[topic];
                this.store[id].Add(new SubscriptionStoreData(subscription, processor));

                return true;
            }

            return false;
        }

        /// <summary>
        /// Gets the broker subsrciption ids by topic. This is for testing purposes only and should not be used in production code, as it only
        /// simulates the topic wildcard matching typically done by the broker.
        /// </summary>
        /// <param name="topic">The topic that should be matched.</param>
        /// <returns>The matching ids, or null if no matching id is found.</returns>
        public List<uint?> GetBrokerSubscriptionIdByTopic(string topic)
        {
            var result = new List<uint?>();

            foreach (var keyValue in store)
            {
                foreach (var item in keyValue.Value)
                {
                    if (this.TestTopicMatch(topic, item.Subscription.Topic)) result.Add(keyValue.Key);
                }
            }

            return result;
        }

        /// <summary>
        /// Tests whether the topic under test matches the wildcard topic. In case of an invalid wildcard false will be returned.
        /// </summary>
        /// <param name="topicUnderTest">The topic that should be tested, whether it matches the wildcard topic.</param>
        /// <param name="wildcardTopic">The wildcard topic to be matched against.</param>
        /// <returns>A value indicating whether the topic under test matches the wildcard topic. Or false, if the wildcard pattern is invalid.</returns>
        private bool TestTopicMatch(string? topicUnderTest, string? wildcardTopic)
        {
            if (topicUnderTest == null || wildcardTopic == null) return false;

            // Only one # is allowed and must be the last character of the wildcard.
            if (wildcardTopic.Contains('#'))
            {
                if (wildcardTopic.Count('#') != 1) return false;
                if (!wildcardTopic.EndsWith('#')) return false;
            }

            var wildcardPaths = wildcardTopic.Split('/');
            var topicUnderTestPaths = topicUnderTest.Split('/');

            var minCount = Math.Min(wildcardPaths.Length, topicUnderTestPaths.Length);

            for (var i = 0; i < minCount; i++)
            {
                if (wildcardPaths[i] == "#") return true;

                if (wildcardPaths[i] != "+")
                {
                    if (wildcardPaths[i] != topicUnderTestPaths[i]) return false;
                }
            }

            // Not all wildcard paths have been matched yet. This is ok, if the next (and last) wildcard path is the multi-level-wildcard # which
            // includes 0 matches. In all other cases, this is not a match.
            if (wildcardPaths.Length > minCount && wildcardPaths[minCount] != "#") return false;

            // If there are still unmatched paths then this is not a match, as the multi-level-wildcard would have allready been applied.
            if (wildcardPaths.Length < topicUnderTestPaths.Length) return false;

            return true;
        }
    }
}
