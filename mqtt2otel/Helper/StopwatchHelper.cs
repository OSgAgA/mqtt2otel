using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Text;

namespace mqtt2otel.Helper
{
    /// <summary>
    /// A static helper class to provide extension properties/methods for the <see cref="Stopwatch"/> class.
    /// </summary>
    public static class StopwatchHelper
    {
        /// <summary>
        /// Extends a stopwatch.
        /// </summary>
        /// <param name="sw">The stopwatch.</param>
        extension(Stopwatch sw)
        {
            /// <summary>
            /// Gets the elapsed microseconds.
            /// </summary>
            public double ElapsedMicroseconds => (double)sw.ElapsedTicks / Stopwatch.Frequency * 1000 * 1000;
        }
    }
}
