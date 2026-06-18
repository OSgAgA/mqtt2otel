using System;
using System.Collections.Generic;
using System.Text;

namespace mqtt2otel.Metadata
{
    /// <summary>
    /// Represents the expectation for a metric point.
    /// </summary>
    /// <param name="value">The value of the metric point.</param>
    /// <param name="startTime">The start of the time range, where this value is valid.</param>
    /// <param name="endTime">The end of the time range, where this value is valid.</param>
    public class MetricPointTestData( object value, DateTimeOffset startTime, DateTimeOffset endTime)
    {
        /// <summary>
        /// Gets or sets the value of the metric point.
        /// </summary>
        public object Value { get; set; } = value;

        /// <summary>
        /// Gets or sets the start of the time range, where this value is valid.
        /// </summary>
        public DateTimeOffset StartTime { get; set; } = startTime;

        /// <summary>
        /// Gets or sets the end of the time range, where this value is valid.
        /// </summary>
        public DateTimeOffset EndTime { get; set; } = endTime;

        /// <summary>
        /// Gets or sets the tags associated with the metric point.
        /// </summary>
        public Dictionary<string, object?> Tags { get; set; } = new();
    }
}
