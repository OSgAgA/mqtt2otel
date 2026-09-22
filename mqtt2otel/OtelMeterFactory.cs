using mqtt2otel.Helper;
using mqtt2otel.Manifest;
using Newtonsoft.Json;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Runtime.CompilerServices;
using System.Text;

namespace mqtt2otel
{
    /// <summary>
    /// Responsible for creating and managing new open telemetry meters.
    /// </summary>
    public class OtelMeterFactory
    {
        /// <summary>
        /// An internal list of all already created meters.
        /// 
        /// The format is meters[connectionName][meterName].
        /// </summary>
        private Dictionary<string, Dictionary<string, System.Diagnostics.Metrics.Meter>> meters = new();

        /// <summary>
        /// Contains information about how to build new meters.
        /// </summary>
        private IEnumerable<OtelScope> meterInfo;

        /// <summary>
        /// Maps a connection to its default meter.
        /// </summary>
        private Dictionary<string, string> connectionToDefaultMapping = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="OtelMeterFactory"/> class.
        /// </summary>
        /// <param name="meterInfo">Information about how to create new meters from the manifest.</param>
        public OtelMeterFactory(IEnumerable<OtelScope> meterInfo)
        {
            this.meterInfo = meterInfo;
        }

        /// <summary>
        /// Gets an existing meter, or creates a new one if the meter does not exist.
        /// </summary>
        /// <param name="metric">The metric for which the meter should be provided.</param>
        /// <returns>The existing or newly created meter.</returns>
        public System.Diagnostics.Metrics.Meter GetOrCreateMeter(OtelMetricRule metric)
        {
            string connectionName = metric.OtelConnection ?? "unknown connection";

            if (!meters.ContainsKey(connectionName))
            {
                this.meters[connectionName] = new Dictionary<string, System.Diagnostics.Metrics.Meter>();
            }

            if (metric.OtelScope == null && !this.connectionToDefaultMapping.ContainsKey(connectionName))
            {
                var query = this.meterInfo.Where(info => info.OtelConnection == connectionName);

                if (!query.Any())
                {
                    string defaultMeterName = "mqtt2otel";
                    this.meters[connectionName][defaultMeterName] = new Meter(name: defaultMeterName, version: "1.0.0");
                    this.connectionToDefaultMapping[connectionName] = defaultMeterName;
                }
                else
                {
                    var info = query.First();
                    this.meters[connectionName][info.Name] = new Meter(name: info.Name, version: info.Version, tags: info.Attributes.ToKeyValuePairs());
                    this.connectionToDefaultMapping[connectionName] = info.Name;
                }
            }

            string meterName = metric.OtelScope ?? this.connectionToDefaultMapping[connectionName];

            // If meter cannot be found, create a new meter.
            if (!this.meters[connectionName].ContainsKey(meterName))
            {
                var query = this.meterInfo.Where(info => info.Name == meterName);

                // Do we have information about the meter?
                if (query.Any())
                {
                    // yes, then use it to create the new query.
                    var info = query.First();
                    this.meters[connectionName][meterName] = new Meter(name: meterName, version: info.Version, tags: info.Attributes.ToKeyValuePairs());
                }
                else
                {
                    // no, so build new query with default values.
                    this.meters[connectionName][meterName] = new Meter(name: meterName, version: "1.0.0");
                }
            }

            return this.meters[connectionName][meterName];
        }

        /// <summary>
        /// Gets all meters names for the provided connection.
        /// </summary>
        /// <param name="connectionName">The name of the connection.</param>
        /// <returns>A list of meter names.</returns>
        public List<string> GetMeterNamesForConnection(string connectionName)
        {
            if (this.meters.ContainsKey(connectionName))
            {
                return this.meters[connectionName].Select(keyValue => keyValue.Key).ToList();
            }
            else
            {
                return new List<string>();
            }
        }

        /// <summary>
        /// Disposes all currently managed meters.
        /// </summary>
        public void DisposeMeters()
        {
            foreach (var keyValue in this.meters)
            {
                foreach (var meter in keyValue.Value.Values)
                {
                    meter.Dispose();
                }
            }

            this.meters = new();
        }
    }
}