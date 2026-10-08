using System;
using System.Collections.Generic;
using System.Text;

namespace mqtt2otel.ManifestExplorer.Tests.Helper
{
    /// <summary>
    /// Represents the supported browser engines.
    /// </summary>
    public enum BrowserEngine
    {
        /// <summary>
        /// The chromium browsers, like chrome and edge.
        /// </summary>
        Chromium,

        /// <summary>
        /// The webkit based browsers like safari.
        /// </summary>
        Webkit,

        /// <summary>
        /// The fireforx browsers.
        /// </summary>
        Firefox
    }
}
