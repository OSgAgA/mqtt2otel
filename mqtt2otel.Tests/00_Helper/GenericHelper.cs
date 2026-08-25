using Microsoft.Extensions.Logging;
using Moq;
using mqtt2otel.Helper;
using mqtt2otel.Interfaces;
using mqtt2otel.Manifest;
using mqtt2otel.Parser;
using mqtt2otel.Stores;
using mqtt2otel.Transformation;
using System;
using System.Collections.Generic;
using System.Text;

namespace mqtt2otel.Tests.Helper
{
    public static class GenericHelper
    {
        /// <summary>
        /// Gets an empty <see cref="DataStores"/> object.
        /// </summary>
        /// <returns>The created data stores.</returns>
        public static DataStores GetDataStores(PayloadParser? payloadParser = null, EmbeddedExpressionParser? embeddedExpressionParser = null)
        {
            if (payloadParser == null)
            {
                payloadParser = new PayloadParser();
            }

            if (embeddedExpressionParser == null)
            {
                embeddedExpressionParser = new EmbeddedExpressionParser(payloadParser);
            }

            var signalStore = new SignalStore(embeddedExpressionParser);
            var internalLogger = new Mock<ILogger<string>>();
            var loggerStore = new LoggerStore(internalLogger.Object, payloadParser, new PayloadTransformation(), embeddedExpressionParser);

            return new DataStores(signalStore, loggerStore);
        }

        /// <summary>
        /// Writes a metric to the signal store.
        /// </summary>
        /// <typeparam name="T">The data type of the metric point.</typeparam>
        /// <param name="subscription">the subscription that triggered the write.</param>
        /// <param name="rule">The rule that triggered the write.</param>
        /// <param name="name">The instrument name.</param>
        /// <param name="store">The signal store where the metric should be written to.</param>
        /// <param name="value">The value of the created metric point.</param>
        /// <param name="description">An optional description text.</param>
        /// <param name="unit">An optional unit.</param>
        /// <param name="attributes">Optional open telemetry attributes.</param>
        public static void WriteMetricToSignalStore<T>(MqttSubscription subscription, OtelMetricRule rule, string name, ISignalStore store, T value, string description = "", string unit = "", IEnumerable<OtelAttribute>? attributes = null)
        {
            if (attributes == null ) attributes = rule.Attributes;

            var expectedMetric = new OtelMetric<T>(value, description, unit, attributes);
            store.UpdateValue<T>(subscription, rule, new ParsingContext(new List<Variable>(), new MqttMessage()), value, attributes);
        }
    }
}
