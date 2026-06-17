using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using System;
using System.Collections.Generic;
using System.Text;

namespace mqtt2otel.Metadata
{
    public class ExpectedTestResult
    {
        public List<MetricTestData> Metrics { get; set; } = new();

        public List<LogTestData> Logs { get; set; } = new();

        public ExpectedTestResult() { }

        public ExpectedTestResult(List<MetricTestData> metrics, List<LogTestData> logs)
        {
            this.Metrics = metrics;
            this.Logs = logs;
        }
    }
}