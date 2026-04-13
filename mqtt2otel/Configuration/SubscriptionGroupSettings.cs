using System;
using System.Collections.Generic;
using System.Text;

namespace mqtt2otel.Configuration
{
    /// <summary>
    /// Provides the settings for subscription groups. Subscription groups bundle multiple subscriptions to make them available
    /// in different contexts.
    /// </summary>
    public class SubscriptionGroupSettings
    {
        /// <summary>
        /// Gets or sets the name of the subscription group.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a subpath that should be added to the end of the subscription topic.
        /// </summary>
        public string SubPath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a parent path that should be added at the beginning of the subscription topic.
        /// </summary>
        public string ParentPath { get; set; } = string.Empty;
    }
}
