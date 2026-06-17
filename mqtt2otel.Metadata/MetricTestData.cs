using OpenTelemetry.Metrics;
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace mqtt2otel.Metadata
{
    public class MetricTestData
    {
        public string Name { get; set; } = string.Empty;

        public string OtelServer { get; set; } = string.Empty;

        public string MeterVersion { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string Unit { get; set; } = string.Empty;

        public MetricType MetricType { get; set; } = new MetricType();

        public List<MetricPointTestData> MetricPoints { get; set; } = new();

        public MetricTestData() { }

        public MetricTestData(Metric metric)
        {
            this.Name = metric.Name;
            this.OtelServer = metric.MeterName;
            this.MeterVersion = metric.MeterVersion;
            this.Description = metric.Description;
            this.Unit = metric.Unit;
            this.MetricType = metric.MetricType;

            foreach (var metricPoint in metric.GetMetricPoints())
            {
                object value = "";

                switch (metric.MetricType)
                {
                    case MetricType.LongSum:
                        value = metricPoint.GetSumLong();
                        break;
                    case MetricType.DoubleSum:
                        value = metricPoint.GetSumDouble();
                        break;
                    case MetricType.LongGauge:
                        value = metricPoint.GetGaugeLastValueLong();
                        break;
                    case MetricType.DoubleGauge:
                        value = metricPoint.GetGaugeLastValueDouble();
                        break;
                    case MetricType.Histogram:
                        value = metricPoint.GetHistogramSum();
                        break;
                    case MetricType.ExponentialHistogram:
                        value = metricPoint.GetExponentialHistogramData();
                        break;
                    case MetricType.LongSumNonMonotonic:
                        value = metricPoint.GetSumLong();
                        break;
                    case MetricType.DoubleSumNonMonotonic:
                        value = metricPoint.GetSumDouble();
                        break;
                }

                var point = new MetricPointTestData(value, metricPoint.StartTime, metricPoint.EndTime);

                if (metricPoint.Tags.Count > 0)
                {
                    foreach (var tag in metricPoint.Tags)
                    {

                        point.Tags[tag.Key] = tag.Value;
                    }
                }

                this.MetricPoints.Add(point);
            }
        }

        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.Append("Name: ");
            sb.AppendLine(this.Name);

            sb.Append("  Otel server: ");
            sb.AppendLine(this.OtelServer);


            sb.Append("  Meter version: ");
            sb.AppendLine(this.MeterVersion);

            sb.Append("  Description: ");
            sb.AppendLine(this.Description);

            sb.Append("  Unit: ");
            sb.AppendLine(this.Unit);

            sb.Append("  Metric type: ");
            sb.AppendLine(this.MetricType.ToString());

            sb.AppendLine("  Metric points: ");
            foreach (var metricPoint in this.MetricPoints)
            {
                sb.Append("    Value:");
                sb.AppendLine(" " + metricPoint.Value.ToString());

                sb.AppendLine($"    Start time: {metricPoint.StartTime}");
                sb.AppendLine($"    End time: {metricPoint.EndTime}");

                sb.AppendLine("    Attributes:");
                foreach (var tag in metricPoint.Tags)
                {
                    sb.AppendLine("      " + tag.Key + ": " + tag.Value);
                }
            }

            return sb.ToString();
        }
    }
}
