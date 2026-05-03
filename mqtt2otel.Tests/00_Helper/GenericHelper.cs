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
        public static DataStores GetDataStores()
        {
            var signalStore = new SignalStore();
            var loggerStore = new LoggerStore(new PayloadParser(), new PayloadTransformation());

            return new DataStores(signalStore, loggerStore);
        }

        public static void WriteMetricToSignalStore<T>(MqttSubscription subscription, OtelMetricRule rule, ISignalStore store, T value, string description = "", string unit = "", IEnumerable<Variable>? attributes = null)
        {
            if (attributes == null ) attributes = rule.Attributes;

            var expectedMetric = new OtelMetric<T>(value, description, unit, attributes);
            store.StoreValue(subscription.Id, rule.Id, expectedMetric);
        }
    }
}
