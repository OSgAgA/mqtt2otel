using OpenTelemetry;
using OpenTelemetry.Metrics;
using System;
using System.Collections.Generic;
using System.Text;

namespace mqtt2otel.Helper
{
    public class DebugMetricReader : MetricReader
    {
        protected override bool OnCollect(int timeoutMilliseconds)
        {
            return base.OnCollect(timeoutMilliseconds);
        }

        protected override bool OnShutdown(int timeoutMilliseconds)
        {
            return base.OnShutdown(timeoutMilliseconds);
        }
    }

}
