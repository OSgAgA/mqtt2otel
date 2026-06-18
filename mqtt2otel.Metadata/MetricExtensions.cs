using OpenTelemetry.Metrics;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using YamlDotNet.Core.Tokens;

namespace mqtt2otel.Metadata
{
    /// <summary>
    /// Provides static extension methods for metrics.
    /// </summary>
    public static class MetricExtensions
    {
        /// <summary>
        /// Gets the value of a metric point as an object.
        /// </summary>
        /// <param name="metricPoint">The metric point, that contains the value.</param>
        /// <param name="metricType">The type of the metric.</param>
        /// <returns>The value as an object.</returns>
        /// <exception cref="Exception">Thrown if the metric type is not supported.</exception>
        public static object GetValueAsObject(this MetricPoint metricPoint, MetricType metricType)
        {
            switch (metricType)
            {
                case MetricType.LongSum:
                    return metricPoint.GetSumLong();
                case MetricType.DoubleSum:
                    return metricPoint.GetSumDouble();
                case MetricType.LongGauge:
                    return metricPoint.GetGaugeLastValueLong();
                case MetricType.DoubleGauge:
                    return metricPoint.GetGaugeLastValueDouble();
                case MetricType.Histogram:
                    return metricPoint.GetHistogramSum();
                case MetricType.ExponentialHistogram:
                    return metricPoint.GetExponentialHistogramData();
                case MetricType.LongSumNonMonotonic:
                    return metricPoint.GetSumLong();
                case MetricType.DoubleSumNonMonotonic:
                    return metricPoint.GetSumDouble();
            }

            throw new Exception($"Unsupported metric type: {metricType}");
        }
    }
}
