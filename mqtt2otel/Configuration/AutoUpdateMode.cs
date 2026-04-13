using System;
using System.Collections.Generic;
using System.Text;

namespace mqtt2otel.Configuration
{
    /// <summary>
    /// Represents the available modes for automatically updating the application if the manifest file has changed.
    /// </summary>
    public enum AutoUpdateMode
    {
        /// <summary>
        /// Perform no wutomatic update.
        /// </summary>
        None,

        /// <summary>
        /// The file will be watched for changes. Only working if the filesystem supports this.
        /// </summary>
        WatchManifestFile,

        /// <summary>
        /// The file will be checked every x seconds whether it has changed.
        /// </summary>
        PollManifestFile
    }
}
