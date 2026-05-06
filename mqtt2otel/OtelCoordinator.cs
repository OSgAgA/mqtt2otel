using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using mqtt2otel.Manifest;
using mqtt2otel.Helper;
using mqtt2otel.InternalLogging;
using mqtt2otel.Parser;
using mqtt2otel.Stores;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using mqtt2otel.Interfaces;
using mqtt2otel.InternalMetrics;

namespace mqtt2otel
{
    /// <summary>
    /// The main class for communicating with the open telemetry endpoint
    /// </summary>
    public class OtelCoordinator : IOtelCoordinator
    {
        /// <summary>
        /// The activity source used by the coordinator for tracing.
        /// </summary>
        public readonly ActivitySource ActivitySource = new("mqtt2otel");

        /// <summary>
        /// Gets the logger factory map, that maps the otel connection name to the loggerFactory used for creating otel loggers.
        /// </summary>
        private Dictionary<string, ILoggerFactory> loggerFactoryMap = new();

        /// <summary>
        /// The data stores used by the application to exchange data asynchronously.
        /// </summary>
        private IDataStores dataStores;

        /// <summary>
        /// Gets or sets a tracer provider.
        /// </summary>
        private TracerProvider? tracerProvider { get; set; } = null;

        /// <summary>
        /// Gets or sets a map that will map an otel connection name to a created otel meter.
        /// </summary>
        private Dictionary<string, Meter> MeterConnectionMap { get; set; } = new();

        /// <summary>
        /// Ensures that the meter providers will not get garbage collected.
        /// </summary>
        private List<MeterProvider> MeterProviders { get; set; } = new();

        /// <summary>
        /// The logger used for internal logging.
        /// </summary>
        private ILogger<OtelCoordinator> internalLogger;

        /// <summary>
        /// The exporter builder for creating open telemetry exporters.
        /// </summary>
        private IOtelExporterBuilder exporterBuilder;

        /// <summary>
        /// The meter for reporting internal metrics.
        /// </summary>
        private OtelMeter otelMeter;

        /// <summary>
        /// Initializes a new instance of the <see cref="OtelCoordinator"/> class.
        /// </summary>
        /// <param name="internalLogger">The logger used for internal logging.</param>
        /// <param name="exporterBuilder">The exporter builder for creating open telemetry exporters.</param>
        /// <param name="dataStores">The data stores used by the application to exchange data asynchronously.</param>
        /// <param name="meter">The meter for reporting internal metrics.</param>
        public OtelCoordinator(ILogger<OtelCoordinator> internalLogger, IOtelExporterBuilder exporterBuilder, IDataStores dataStores, OtelMeter meter)
        {
            this.otelMeter = meter;
            this.internalLogger = internalLogger;
            this.exporterBuilder = exporterBuilder;
            this.dataStores = dataStores;
        }

        /// <summary>
        /// Connects to the server as described in the manifest and prepares all metrics and loggers.
        /// </summary>
        /// <param name="manifest">The manifest, contiaining the connection information.</param>
        public void Connect(Manifest.Manifest manifest)
        {
            foreach (var otelConnection in manifest.OtelConnections)
            {
                string name = otelConnection.Name;

                if (string.IsNullOrWhiteSpace(name)) name = "Default";

                using (this.internalLogger.StartActivity($"Otel connection information for: {otelConnection.Name}"))
                {
                    this.internalLogger.LogInformation("Otel endpoint:              {OtelEndpoint}", otelConnection.Endpoint.FullAddress);
                    this.internalLogger.LogInformation("Otel export protocoll:      {OtelExportProtocol}", otelConnection.OtlpExportProtocol);
                    this.internalLogger.LogInformation("Otel export processor type: {OtelExportProcessorType}", otelConnection.ExportProcessorType);
                    this.internalLogger.LogInformation("Otel service name:          {OtelServiceNamespace}", otelConnection.ServiceName);
                    this.internalLogger.LogInformation("Otel service version:       {OtelServicVersion}", otelConnection.ServiceVersion);
                    this.internalLogger.LogInformation("Otel service namespace:     {OtelServiceNamespace}", otelConnection.ServiceNamespace);
                }
            }

            this.otelMeter.Connections.Record(manifest.OtelConnections.Count);

            this.internalLogger.LogInformation("Initializing otel meters...");
            this.InitializeMeters(manifest);
            this.internalLogger.LogInformation("Otel meters initialization successful.");

            this.internalLogger.LogInformation("Initializing otel logging...");
            this.InitializeLogging(manifest);
            this.internalLogger.LogInformation("Otel logging initialization successful.");
        }

        /// <summary>
        /// Forces a flush for all meter providers.
        /// </summary>
        public void FlushMeters()
        {
            foreach (var meterProvider in this.MeterProviders)
            {
                meterProvider.ForceFlush();
            }
        }

        /// <summary>
        /// Initializes the otel meters based on the provided settings.
        /// </summary>
        /// <param name="manifest">The rules for creating meters.</param>
        private void InitializeMeters(Manifest.Manifest manifest)
        {

            // Create a separate meter for each connection.
            foreach (var otelConnection in manifest.OtelConnections)
            {
                var meter = new Meter(otelConnection.Name);

                this.MeterConnectionMap[otelConnection.Name] = meter;
            }

            // Create all instruments
            foreach (var processor in manifest.Processors)
            {
                foreach (var subscriptionSettings in processor.Mqtt.Subscriptions)
                {
                    this.CreateInstruments(processor, subscriptionSettings);
                }
            }

            // Create meter providers
            foreach (var otelConnection in manifest.OtelConnections)
            {
                var builder = Sdk.CreateMeterProviderBuilder()
                    .SetResourceBuilder(
                            ResourceBuilder.CreateDefault()
                            .AddService(otelConnection.ServiceName, serviceNamespace: otelConnection.ServiceNamespace))
                    .AddMeter(otelConnection.Name);

                this.exporterBuilder.AddToMeterProviderBuilder(builder, otelConnection);

                var provider = builder.Build();

                this.MeterProviders.Add(provider);
            }
        }

        /// <summary>
        /// Create and store all otel meter instruments based on the given rules.
        /// </summary>
        /// <param name="processor">The processor defining how the meter is created.</param>
        /// <param name="mqttSubscription">The mqtt subscription with which the meter is connected.</param>
        /// <exception cref="ArgumentNullException">Thrown if a subscription rule is defined without a name.</exception>
        /// <exception cref="Exception">Thrown if instrument does not exist.</exception>
        private void CreateInstruments(Processor processor, MqttSubscription mqttSubscription)
        {
            foreach (var rulesSettings in processor.Otel.Metrics)
            {
                if (rulesSettings.Name == null) throw new ArgumentNullException(nameof(rulesSettings));

                if (rulesSettings.OtelConnection == null || !this.MeterConnectionMap.ContainsKey(rulesSettings.OtelConnection))
                {
                    throw new Exception($"Cannot create instrument. No meter exists for metric rule with name: {rulesSettings.Name}.");
                }

                string expandedName = VariableParser.Expand(rulesSettings.Name, mqttSubscription.Variables);

                string key = mqttSubscription.Id + ":" + rulesSettings.Id;

                string instrumentCreationMethodName = string.Empty;

                switch (rulesSettings.Instrument)
                {
                    case OtelMetricInstrument.Gauge:
                        instrumentCreationMethodName = nameof(CreateGauge);
                        break;
                    case OtelMetricInstrument.AsynchronousGauge:
                        instrumentCreationMethodName = nameof(CreateAsynchronousGauge);
                        break;
                    case OtelMetricInstrument.Counter:
                        instrumentCreationMethodName = nameof(CreateCounter);
                        break;
                    case OtelMetricInstrument.AsynchronousCounter:
                        instrumentCreationMethodName = nameof(CreateAsynchronousCounter);
                        break;
                    case OtelMetricInstrument.UpDownCounter:
                        instrumentCreationMethodName = nameof(CreateUpDownCounter);
                        break;
                    case OtelMetricInstrument.AsynchronousUpDownCounter:
                        instrumentCreationMethodName = nameof(CreateAsynchronousUpDownCounter);
                        break;
                    case OtelMetricInstrument.Histogram:
                        instrumentCreationMethodName = nameof(CreateHistogram);
                        break;
                    default:
                        throw new Exception($"Unsupported otel metric type: '{rulesSettings.Instrument.ToString()}' for metric {processor.Name}.");
                }

                var meter = this.MeterConnectionMap[rulesSettings.OtelConnection];
                TypeHelper.CallMethodWithGenericType(this, rulesSettings.SignalDataType, instrumentCreationMethodName, new object[] { rulesSettings, mqttSubscription, key, meter, expandedName });
            }
        }

        /// <summary>
        /// Initializes open telemetry logging based on the provided manifest.
        /// </summary>
        /// <param name="manifest">The manifest providing the logging configuration and the logging rules.</param>
        /// <exception cref="Exception">Thrown if no otel server endpoint is defined.</exception>
        private void InitializeLogging(Manifest.Manifest manifest)
        {
            // Create log factories for each connection
            foreach (var otelConnection in manifest.OtelConnections)
            {
                this.loggerFactoryMap[otelConnection.Name] = Microsoft.Extensions.Logging.LoggerFactory.Create(builder =>
                {
                    builder.AddOpenTelemetry(options =>
                    {
                        options.SetResourceBuilder(
                            ResourceBuilder.CreateDefault()
                            .AddService(otelConnection.ServiceName, serviceNamespace: otelConnection.ServiceNamespace))
                            .IncludeScopes = true;
                        options.AddProcessor(new TimestampOverrideProcessor(this.otelMeter));

                        this.exporterBuilder.AddToLoggerOptions(options, otelConnection);
                    });
                });
            }

            // Prrocess logging rules
            foreach (var processor in manifest.Processors)
            {
                foreach (var otelLoggingRule in processor.Otel.Logs)
                {
                    if (otelLoggingRule.OtelConnection == null) throw new Exception($"Internal error: {nameof(otelLoggingRule.OtelConnection)} must not be null for logging rule: {otelLoggingRule.Name}");

                    var logger = this.loggerFactoryMap[otelLoggingRule.OtelConnection].CreateLogger(otelLoggingRule.CategoryName);
                    this.dataStores.LoggerStore.StoreLogger(otelLoggingRule.Id, logger);
                }
            }
        }

        /// <summary>
        /// Creates an asynchronous gauge instrument and the corresponding signal store entry.
        /// </summary>
        /// <typeparam name="T">The type of the signal stored.</typeparam>
        /// <param name="otelMetricRule">The rule defining the metric.</param>
        /// <param name="mqttSubscription">The subscription for connecting the instrument with the subscription.</param>
        /// <param name="key">The key under which the value will be stored in the signal store.</param>
        /// <param name="meter">The meter to which this instrument should be added.</param>
        private void CreateAsynchronousGauge<T>(OtelMetricRule otelMetricRule, MqttSubscription mqttSubscription, string key, Meter meter, string expandedName) where T : struct
        {
            this.dataStores.SignalStore.StoreValue<T>(mqttSubscription.Id, otelMetricRule.Id, new OtelMetric<T>(default(T), description: otelMetricRule.Description, otelMetricRule.Unit, new List<Variable>()));
            meter.CreateObservableGauge<T>(
                expandedName,
                () => this.CreateMeasurement<T>(mqttSubscription, otelMetricRule),
                unit: otelMetricRule.Unit,
                description: otelMetricRule.Description);
        }

        /// <summary>
        /// Creates a synchronous gauge instrument and the corresponding signal store entry.
        /// </summary>
        /// <typeparam name="T">The type of the signal stored.</typeparam>
        /// <param name="otelMetricRule">The rule defining the metric.</param>
        /// <param name="mqttSubscription">The subscription for connecting the instrument with the subscription.</param>
        /// <param name="expandedName">The name that will identify the gauge. The name should already have all variables expanded.</param>
        /// <param name="key">The key under which the value will be stored in the signal store.</param>
        /// <param name="meter">The meter to which this instrument should be added.</param>
        private void CreateGauge<T>(OtelMetricRule otelMetricRule, MqttSubscription mqttSubscription, string key, Meter meter, string expandedName) where T : struct
        {
            this.dataStores.SignalStore.StoreValue<T>(mqttSubscription.Id, otelMetricRule.Id, new OtelMetric<T>(default(T), description: otelMetricRule.Description, otelMetricRule.Unit, new List<Variable>()));
            var gauge = meter.CreateGauge<T>(
                expandedName,
                unit: otelMetricRule.Unit,
                description: otelMetricRule.Description);
            this.dataStores.SignalStore.RegisterCallback(mqttSubscription.Id, otelMetricRule.Id, signalStoreKey => this.RecordAttributedValue<T>(mqttSubscription, otelMetricRule, (value, attributes) => gauge.Record(value, attributes)));
        }

        /// <summary>
        /// Creates an asynchronous counter instrument and the corresponding signal store entry.
        /// </summary>
        /// <typeparam name="T">The type of the signal stored.</typeparam>
        /// <param name="otelMetricRule">The rule defining the metric.</param>
        /// <param name="mqttSubscription">The subscription for connecting the instrument with the subscription.</param>
        /// <param name="key">The key under which the value will be stored in the signal store.</param>
        /// <param name="meter">The meter to which this instrument should be added.</param>
        private void CreateAsynchronousCounter<T>(OtelMetricRule otelMetricRule, MqttSubscription mqttSubscription, string key, Meter meter, string expandedName) where T : struct
        {
            this.dataStores.SignalStore.StoreValue<T>(mqttSubscription.Id, otelMetricRule.Id, new OtelMetric<T>(default(T), description: otelMetricRule.Description, otelMetricRule.Unit, new List<Variable>()));
            meter.CreateObservableCounter<T>(
                expandedName,
                () => this.CreateMeasurement<T>(mqttSubscription, otelMetricRule),
                unit: otelMetricRule.Unit,
                description: otelMetricRule.Description);
        }

        /// <summary>
        /// Creates an synchronous counter instrument and the corresponding signal store entry.
        /// </summary>
        /// <typeparam name="T">The type of the signal stored.</typeparam>
        /// <param name="otelMetricRule">The rule defining the metric.</param>
        /// <param name="mqttSubscription">The subscription for connecting the instrument with the subscription.</param>
        /// <param name="key">The key under which the value will be stored in the signal store.</param>
        /// <param name="meter">The meter to which this instrument should be added.</param>
        private void CreateCounter<T>(OtelMetricRule otelMetricRule, MqttSubscription mqttSubscription, string key, Meter meter, string expandedName) where T : struct
        {
            this.dataStores.SignalStore.StoreValue<T>(mqttSubscription.Id, otelMetricRule.Id, new OtelMetric<T>(default(T), description: otelMetricRule.Description, otelMetricRule.Unit, new List<Variable>()));
            var counter = meter.CreateCounter<T>(
                expandedName,
                unit: otelMetricRule.Unit,
                description: otelMetricRule.Description);
            this.dataStores.SignalStore.RegisterCallback(mqttSubscription.Id, otelMetricRule.Id, signalStoreKey => this.RecordAttributedValue<T>(mqttSubscription, otelMetricRule, (value, attributes) => counter.Add(value, attributes)));
        }

        /// <summary>
        /// Creates an asynchronous UpDownCounter instrument and the corresponding signal store entry.
        /// </summary>
        /// <typeparam name="T">The type of the signal stored.</typeparam>
        /// <param name="otelMetricRule">The rule settings defining the metric.</param>
        /// <param name="mqttSubscription">The subscription settings for connecting the instrument with the subscription.</param>
        /// <param name="key">The key under which the value will be stored in the signal store.</param>
        /// <param name="meter">The meter to which this instrument should be added.</param>
        private void CreateAsynchronousUpDownCounter<T>(OtelMetricRule otelMetricRule, MqttSubscription mqttSubscription, string key, Meter meter, string expandedName) where T : struct
        {
            this.dataStores.SignalStore.StoreValue<T>(mqttSubscription.Id, otelMetricRule.Id, new OtelMetric<T>(default(T), description: otelMetricRule.Description, otelMetricRule.Unit, new List<Variable>()));
            meter.CreateObservableUpDownCounter<T>(
                expandedName,
                () => this.CreateMeasurement<T>(mqttSubscription, otelMetricRule),
                unit: otelMetricRule.Unit,
                description: otelMetricRule.Description);
        }

        /// <summary>
        /// Creates a synchronous UpDownCounter instrument and the corresponding signal store entry.
        /// </summary>
        /// <typeparam name="T">The type of the signal stored.</typeparam>
        /// <param name="otelMetricRule">The rule defining the metric.</param>
        /// <param name="mqttSubscription">The subscription for connecting the instrument with the subscription.</param>
        /// <param name="key">The key under which the value will be stored in the signal store.</param>
        /// <param name="meter">The meter to which this instrument should be added.</param>
        private void CreateUpDownCounter<T>(OtelMetricRule otelMetricRule, MqttSubscription mqttSubscription, string key, Meter meter, string expandedName) where T : struct
        {
            this.dataStores.SignalStore.StoreValue<T>(mqttSubscription.Id, otelMetricRule.Id, new OtelMetric<T>(default(T), description: otelMetricRule.Description, otelMetricRule.Unit, new List<Variable>()));
            var counter = meter.CreateUpDownCounter<T>(
                expandedName,
                unit: otelMetricRule.Unit,
                description: otelMetricRule.Description);
            this.dataStores.SignalStore.RegisterCallback(mqttSubscription.Id, otelMetricRule.Id, signalStoreKey => this.RecordAttributedValue<T>(mqttSubscription, otelMetricRule, (value, attributes) => counter.Add(value, attributes)));
        }

        /// <summary>
        /// Creates a synchronous histogram instrument and the corresponding signal store entry.
        /// </summary>
        /// <typeparam name="T">The type of the signal stored.</typeparam>
        /// <param name="otelMetricRule">The rule defining the metric.</param>
        /// <param name="mqttSubscription">The subscription for connecting the instrument with the subscription.</param>
        /// <param name="key">The key under which the value will be stored in the signal store.</param>
        /// <param name="meter">The meter to which this instrument should be added.</param>
        private void CreateHistogram<T>(OtelMetricRule otelMetricRule, MqttSubscription mqttSubscription, string key, Meter meter, string expandedName) where T : struct
        {
            this.dataStores.SignalStore.StoreValue<T>(mqttSubscription.Id, otelMetricRule.Id, new OtelMetric<T>(default(T), description: otelMetricRule.Description, otelMetricRule.Unit, new List<Variable>()));

            Histogram<T>? histogram = null;

            if (otelMetricRule.HistogramBucketBoundaries != null && otelMetricRule.HistogramBucketBoundaries.Count > 0)
            {
                var list = TypeHelper.Parse<T>(otelMetricRule.HistogramBucketBoundaries);
                var readonlyList = list.AsReadOnly<T>();
                var advice = new InstrumentAdvice<T>() { HistogramBucketBoundaries = readonlyList };
                histogram = meter.CreateHistogram<T>(
                    expandedName,
                    unit: otelMetricRule.Unit,
                    description: otelMetricRule.Description,
                    advice: advice);
            }
            else
            {
                histogram = meter.CreateHistogram<T>(
                    expandedName,
                    unit: otelMetricRule.Unit,
                    description: otelMetricRule.Description);
            }

            if (histogram != null)
            {
                this.dataStores.SignalStore.RegisterCallback(mqttSubscription.Id, otelMetricRule.Id, signalStoreKey => this.RecordAttributedValue<T>(mqttSubscription, otelMetricRule, (value, attributes) => histogram.Record(value, attributes)));
            }
        }

        /// <summary>
        /// Transforms a value from the signal store into a measurement to be used by the open telementry instruments.
        /// </summary>
        /// <typeparam name="TPayload">The type of the value.</typeparam>
        /// <param name="mqttSubscription">The subscription for connecting the instrument with the subscription.</param>
        /// <param name="otelMetricRule">The rule defining the metric.</param>
        /// <returns>The created measurement.</returns>
        private Measurement<TPayload> CreateMeasurement<TPayload>(MqttSubscription mqttSubscription, OtelMetricRule otelMetricRule) where TPayload : struct
        {
            var metric = this.dataStores.SignalStore.GetValue<TPayload>(mqttSubscription.Id, otelMetricRule.Id);

            this.internalLogger.LogDebug($"Providing measurement ({metric.Value}) with attributes ({string.Join(",", metric.Attributes.Select(attribute => attribute.Key + ": " + attribute.Value))}).");

            return new Measurement<TPayload>(
                value: metric.Value,
                tags: metric.Attributes.ToKeyValuePairs()
            );
        }

        /// <summary>
        /// Record a value stored in the signal store via an instrument including alll provided attributes.
        /// </summary>
        /// <typeparam name="TPayload">The type of the value stored.</typeparam>
        /// <param name="mqttSubscription">The subscription for connecting the instrument with the subscription.</param>
        /// <param name="otelMetricRule">The rule defining the metric.</param>
        /// <param name="record">An action that will record the payload and the provided attributes to the instrument.</param>
        private void RecordAttributedValue<TPayload>(MqttSubscription mqttSubscription, OtelMetricRule otelMetricRule, Action<TPayload, TagList> record) where TPayload : struct
        {
            var metric = this.dataStores.SignalStore.GetValue<TPayload>(mqttSubscription.Id, otelMetricRule.Id);

            this.internalLogger.LogDebug($"Providing measurement ({metric.Value}) with attributes ({string.Join(",", metric.Attributes.Select(attribute => attribute.Key + ": " + attribute.Value))}).");

            record(metric.Value, metric.Attributes.ToTagList());
        }

        /// <summary>
        /// Disposes the class and ensures that no signals are sent to the otel servers anymore.
        /// </summary>
        public void Dispose()
        {
            this.otelMeter.Connections.Record(0);
            this.FlushMeters();

            foreach (var meter in this.MeterConnectionMap.Values)
            {
                meter.Dispose();
            }

            foreach (var meterProvider in this.MeterProviders)
            {
                meterProvider.Dispose();
            }

            foreach (var loggerFactory in this.loggerFactoryMap.Values)
            {
                loggerFactory.Dispose();
            }
        }
    }
}
