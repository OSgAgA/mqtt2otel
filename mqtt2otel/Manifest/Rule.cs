using System;
using System.Collections.Generic;
using System.Text;

namespace mqtt2otel.Manifest
{
    public class Rule : NamedIdObject
    {
        /// <summary>
        /// Gets or sets a value indicating, whether the rule is currently active.
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Gets or sets the name of the open telemetriy connection to be used for this rule. 
        /// Set to null for using the default connection.
        /// </summary>
        [InheritedProperty]
        public string? OtelConnection { get; set; } = null;

        /// <summary>
        /// Gets or sets the open telemetry scope that should be used for the metric.
        /// Set to null to use the default scope of the <see cref="OtelConnection"/>.
        /// </summary>
        [InheritedProperty]
        public string? OtelScope { get; set; } = null;

        /// <summary>
        /// Gets or sets a value indicating whether the mqtt message details should be logged in case of an error. Set to null to use the default value.
        /// </summary>
        [InheritedProperty]
        public bool? LogMessageDetails { get; set; } = null;

        /// <summary>
        /// Gets or sets a value indicating whether the rule can be deactivated, when the error rate exceeds <see cref="MaxErrorRateInPercentage"/>.
        /// Set to null to use default value.
        /// </summary>
        [InheritedProperty]
        public bool? AllowRuleDeactivation { get; set; } = null;

        /// <summary>
        /// Gets or sets a value indicating whether the rule can deactivate topics from executing this rule, when the error rate for the topic is to high.
        /// Set to null to use default value.
        /// </summary>
        [InheritedProperty]
        public bool? AllowRuleDeactivationByTopic { get; set; } = null;

        /// <summary>
        /// Gets or sets a value indicating whether the rule can deactivate topics from executing this rule on a scpecific item (e.g. a property of a json message), when the error rate for the topic is to high.
        /// Set to null to use default value.
        /// </summary>
        [InheritedProperty]
        public bool? AllowRuleDeactivationByItem { get; set; } = null;

        /// <summary>
        /// Gets or sets the maximum number of topics that are blocked for an item / rule. If this threshold is exceeded the whole item or rule is blocked.
        /// </summary>
        [InheritedProperty]
        public int? BlockedTopicLimit { get; set; } = null;

        /// <summary>
        /// Gets all blocked items for this rule.
        /// Use the <see cref="BlockItemGate"/> for accessing.
        /// </summary>
        internal HashSet<string> BlockedItems { get; private set; } = new();

        /// <summary>
        /// Gets all blocked topics for this rule.
        /// Use the <see cref="BlockItemGate"/> for accessing.
        /// </summary>
        internal HashSet<string> BlockedTopics { get; private set; } = new();

        /// <summary>
        /// Gets a dictionary with an item name as the key and a list of topics that are blocked for this item as the value.
        /// Use the <see cref="BlockItemMutex"/> for accessing.
        /// </summary>
        internal Dictionary<string, HashSet<string>> BlockedItemTopics { get; private set; } = new(StringComparer.Ordinal);

        /// <summary>
        /// Gets the lock that can be used to ensure, that access to either <see cref="BlockedItems"/> or <see cref="BlockedItemTopics"/> is thread safe.
        /// </summary>
        internal readonly Lock BlockItemGate = new();
    }
}
