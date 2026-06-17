using System;
using System.Collections.Generic;
using System.Text;

namespace mqtt2otel.Metadata
{
    public class MetricPointTestData( object value, DateTimeOffset startTime, DateTimeOffset endTime)
    {
        public object Value { get; set; } = value;

        public DateTimeOffset StartTime { get; set; } = startTime;

        public DateTimeOffset EndTime { get; set; } = endTime;

        public Dictionary<string, object?> Tags { get; set; } = new();
    }
}
