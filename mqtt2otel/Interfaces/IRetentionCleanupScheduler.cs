using System;
using System.Collections.Generic;
using System.Text;

namespace mqtt2otel.Interfaces
{
    /// <summary>
    /// The class implementing this interface should schedule the retention cleanup process.
    /// </summary>
    public interface IRetentionCleanupScheduler
    {
        /// <summary>
        /// Starts the scheduler.
        /// </summary>
        /// <param name="store">The trace store.</param>
        public void Start(ITraceStore store);

        /// <summary>
        /// Stops the scheduler.
        /// </summary>
        public void Stop();
    }
}
