using System;
using System.Collections.Generic;
using System.Text;

namespace mqtt2otel.ManifestExplorer.Tests
{
    /// <summary>
    /// Represents when an action should be triggered.
    /// </summary>
    public enum ActionTrigger
    {
        /// <summary>
        /// The action should only be triggered on test failure.
        /// </summary>
        OnFailure,

        /// <summary>
        /// The action should always be triggered.
        /// </summary>
        Always,

        /// <summary>
        /// The action should never be triggered.
        /// </summary>
        Never,

        /// <summary>
        /// The action should only be triggered on test success.
        /// </summary>
        OnSuccess
    }
}
