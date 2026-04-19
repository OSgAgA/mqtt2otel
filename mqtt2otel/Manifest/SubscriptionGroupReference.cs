using System;
using System.Collections.Generic;
using System.Text;

namespace mqtt2otel.Manifest
{
    /// <summary>
    /// Provides the a reference to a subscription group.
    /// </summary>
    public class SubscriptionGroupReference
    {
        /// <summary>
        /// Gets or sets the name of the refered subscription group.
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
