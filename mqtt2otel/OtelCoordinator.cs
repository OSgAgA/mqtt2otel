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

namespace mqtt2otel
{
    /// <summary>
    /// The main class for communicating with the open telemetry endpoint
    /// </summary>
    public class OtelCoordinator : IDisposable
    {
        /// <summary>
        /// The activity source used by the coordinator for tracing.
        /// </summary>
        public readonly ActivitySource ActivitySource = new("mqtt2otel");

        /// <summary>
        /// Gets the logger factory map, that maps the otel server name to the loggerFactory used for creating otel loggers.
        /// </summary>
        public Dictionary<string, ILoggerFactory> LoggerFactoryMap { get; private set; } = new();

        /// <summary>
        /// Gets or sets the signal store for receiving metric signals.
        /// </summary>
        private SignalStore signalStore { get; set; }

        /// <summary>
        /// Gets or sets the logger store for storing the created otel loggers.
        /// </summary>
        private LoggerStore loggerStore { get; set; }

        /// <summary>
        /// Gets or sets a tracer provider.
        /// </summary>
        private TracerProvider? tracerProvider { get; set; } = null;

        /// <summary>
        /// Gets or sets a map that will map an otel server name to a created otel meter.
        /// </summary>
        private Dictionary<string, Meter> MeterServerMap { get; set; } = new();

        /// <summary>
        /// Ensures that the meter providers will not get garbage collected.
        /// </summary>
        private List<MeterProvider> MeterProviders { get; set; } = new();

        /// <summary>
        /// The logger used for internal logging.
        /// </summary>
        private ILogger<OtelCoordinator> internalLogger;

        /// <summary>
        /// Initializes a new instance of the <see cref="OtelCoordinator"/> class.
        /// </summary>
        /// <param name="internalLogger">The logger used for internal logging.</param>
        /// <param name="manifest">The manifest.</param>
        /// <param name="signalStore">The signal store to store metric signals.</param>
        /// <param name="loggerStore">The store for storing loggers created by this instance.</param>
        public OtelCoordinator(ILogger<OtelCoordinator> internalLogger, Manifest.Manifest manifest, SignalStore signalStore, LoggerStore loggerStore)
        {
            this.internalLogger = internalLogger;
            this.signalStore = signalStore;
            this.loggerStore = loggerStore;

            foreach (var otelServer in manifest.OtelServer)
            {
                string name = otelServer.Name;

                if (string.IsNullOrWhiteSpace(name)) name = "Default";

                using (this.internalLogger.StartActivity($"Otel connection information for server: {otelServer.Name}"))
                {
                    this.internalLogger.LogInformation("Otel endpoint:              {OtelEndpoint}", otelServer.Endpoint.FullAddress);
                    this.internalLogger.LogInformation("Otel export protocoll:      {OtelExportProtocol}", otelServer.OtlpExportProtocol);
                    this.internalLogger.LogInformation("Otel export processor type: {OtelExportProcessorType}", otelServer.ExportProcessorType);
                    this.internalLogger.LogInformation("Otel service name:          {OtelServiceNamespace}", otelServer.ServiceName);
                    this.internalLogger.LogInformation("Otel service version:       {OtelServicVersion}", otelServer.ServiceVersion);
                    this.internalLogger.LogInformation("Otel service namespace:     {OtelServiceNamespace}", otelServer.ServiceNamespace);
                }
            }

            this.internalLogger.LogInformation("Initializing otel meters...");
            this.InitializeMeters(manifest);
            this.internalLogger.LogInformation("Otel meters initialization successful.");

            this.internalLogger.LogInformation("Initializing otel logging...");
            this.InitializeLogging(manifest);
            this.internalLogger.LogInformation("Otel logging initialization successful.");

        }

        /// <summary>
        /// Initializes the otel meters based on the provided settings.
        /// </summary>
        /// <param name="manifest">The rules for creating meters.</param>
        public void InitializeMeters(Manifest.Manifest manifest)
        {

            // Create a separate meter for each server.
            foreach (var otelServerSettings in manifest.OtelServer)
            {
                var meter = new Meter(otelServerSettings.Name);

                this.MeterServerMap[otelServerSettings.Name] = meter;
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
            foreach (var otelServer in manifest.OtelServer)
            {
                var provider = Sdk.CreateMeterProviderBuilder()
                    .SetResourceBuilder(
                            ResourceBuilder.CreateDefault()
                            .AddService(otelServer.ServiceName, serviceNamespace: otelServer.ServiceNamespace))
                    .AddOtlpExporter(otlpOptions => this.InitializeExporterOptions(otlpOptions, otelServer))
                    .AddMeter(otelServer.Name)
                    .Build();

                this.MeterProviders.Add(provider);
            }
        }

        /// <summary>
        /// Initializes <see cref="OtlpExporterOptions"/> based on the provided settings.
        /// </summary>
        /// <param name="otlpOptions">The options that will be initialized.</param>
        /// <param name="server">The settings defining the options to be applied.</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private OtlpExporterOptions InitializeExporterOptions(OtlpExporterOptions otlpOptions, OtelServer server)
        {
            if (server.Endpoint.Address == null) throw new Exception("Address of Otel server endpoint must be set!");

            otlpOptions.Endpoint = server.Endpoint.Uri;
            otlpOptions.Protocol = server.OtlpExportProtocol;
            otlpOptions.ExportProcessorType = server.ExportProcessorType;

            if(server.Endpoint.Headers != null)
            {
                otlpOptions.Headers = server.Endpoint.Headers;
            }

            if (server.Endpoint.BatchTimeoutInMs != null)
            {
                otlpOptions.TimeoutMilliseconds = server.Endpoint.BatchTimeoutInMs.Value;
            }

            if (server.ClientPrefix != null)
            {
                otlpOptions.UserAgentProductIdentifier = server.ClientPrefix;
            }

            if (server.Endpoint.EnableTls)
            {
                otlpOptions.HttpClientFactory = () =>
                {
                    var handler = new HttpClientHandler();
                    var cert = X509CertificateLoader.LoadPkcs12FromFile(server.Endpoint.ClientCertificatePath, server.Endpoint.ClientCertificatePassword);

                    handler.ClientCertificates.Add(cert);
                    return new HttpClient(handler);
                };
            }

            return otlpOptions;
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

                if (rulesSettings.OtelServerName == null || !this.MeterServerMap.ContainsKey(rulesSettings.OtelServerName))
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

                var meter = this.MeterServerMap[rulesSettings.OtelServerName];
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
            // Create log factories for each server

            foreach (var otelServer in manifest.OtelServer)
            {
                this.LoggerFactoryMap[otelServer.Name] = Microsoft.Extensions.Logging.LoggerFactory.Create(builder =>
                {
                    builder.AddOpenTelemetry(options =>
                    {
                        options.SetResourceBuilder(
                            ResourceBuilder.CreateDefault()
                            .AddService(otelServer.ServiceName, serviceNamespace: otelServer.ServiceNamespace))
                            .IncludeScopes = true;

                        options.AddOtlpExporter(otlpOptions => this.InitializeExporterOptions(otlpOptions, otelServer));

                        options.AddProcessor(new TimestampOverrideProcessor());
                    });
                });
            }

            // Prrocess logging rules

            foreach (var processor in manifest.Processors)
            {
                foreach (var otelLoggingRule in processor.Otel.Logs)
                {
                    if (otelLoggingRule.OtelServerName == null) throw new Exception($"Internal error: OtelServerName must not be null for logging rule: {otelLoggingRule.Name}");

                    var logger = this.LoggerFactoryMap[otelLoggingRule.OtelServerName].CreateLogger(otelLoggingRule.CategoryName);
                    this.loggerStore.StoreLogger(otelLoggingRule.Id, logger);
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
            this.signalStore.StoreValue<T>(key, new OtelMetric<T>(default(T), description: otelMetricRule.Description, otelMetricRule.Unit, new List<Variable>()));
            meter.CreateObservableGauge<T>(
                expandedName,
                () => this.CreateMeasurement<T>(key),
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
            this.signalStore.StoreValue<T>(key, new OtelMetric<T>(default(T), description: otelMetricRule.Description, otelMetricRule.Unit, new List<Variable>()));
            var gauge = meter.CreateGauge<T>(
                expandedName,
                unit: otelMetricRule.Unit,
                description: otelMetricRule.Description);
            this.signalStore.RegisterCallback(key, signalStoreKey => this.RecordAttributedValue<T>(signalStoreKey, (value, attributes) => gauge.Record(value, attributes)));
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
            this.signalStore.StoreValue<T>(key, new OtelMetric<T>(default(T), description: otelMetricRule.Description, otelMetricRule.Unit, new List<Variable>()));
            meter.CreateObservableCounter<T>(
                expandedName,
                () => this.CreateMeasurement<T>(key),
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
            this.signalStore.StoreValue<T>(key, new OtelMetric<T>(default(T), description: otelMetricRule.Description, otelMetricRule.Unit, new List<Variable>()));
            var counter = meter.CreateCounter<T>(
                expandedName,
                unit: otelMetricRule.Unit,
                description: otelMetricRule.Description);
            this.signalStore.RegisterCallback(key, signalStoreKey => this.RecordAttributedValue<T>(signalStoreKey, (value, attributes) => counter.Add(value, attributes)));
        }

        /// <summary>
        /// Creates an asynchronous UpDownCounter instrument and the corresponding signal store entry.
        /// </summary>
        /// <typeparam name="T">The type of the signal stored.</typeparam>
        /// <param name="otelMetricRule">The rule settings defining the metric.</param>
        /// <param name="mqtt">The subscription settings for connecting the instrument with the subscription.</param>
        /// <param name="key">The key under which the value will be stored in the signal store.</param>
        /// <param name="meter">The meter to which this instrument should be added.</param>
        private void CreateAsynchronousUpDownCounter<T>(OtelMetricRule otelMetricRule, MqttSubscription mqtt, string key, Meter meter, string expandedName) where T : struct
        {
            this.signalStore.StoreValue<T>(key, new OtelMetric<T>(default(T), description: otelMetricRule.Description, otelMetricRule.Unit, new List<Variable>()));
            meter.CreateObservableUpDownCounter<T>(
                expandedName,
                () => this.CreateMeasurement<T>(key),
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
            this.signalStore.StoreValue<T>(key, new OtelMetric<T>(default(T), description: otelMetricRule.Description, otelMetricRule.Unit, new List<Variable>()));
            var counter = meter.CreateUpDownCounter<T>(
                expandedName,
                unit: otelMetricRule.Unit,
                description: otelMetricRule.Description);
            this.signalStore.RegisterCallback(key, signalStoreKey => this.RecordAttributedValue<T>(signalStoreKey, (value, attributes) => counter.Add(value, attributes)));
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
            this.signalStore.StoreValue<T>(key, new OtelMetric<T>(default(T), description: otelMetricRule.Description, otelMetricRule.Unit, new List<Variable>()));

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
                this.signalStore.RegisterCallback(key, signalStoreKey => this.RecordAttributedValue<T>(signalStoreKey, (value, attributes) => histogram.Record(value, attributes)));
            }
        }

        /// <summary>
        /// Transforms a value from the signal store into a measurement to be used by the open telementry instruments.
        /// </summary>
        /// <typeparam name="TPayload">The type of the value.</typeparam>
        /// <param name="signalStoreKey">The key under which the value can be retrieved from the signal store.</param>
        /// <returns>The created measurement.</returns>
        private Measurement<TPayload> CreateMeasurement<TPayload>(string signalStoreKey) where TPayload : struct
        {
            var metric = this.signalStore.GetValue<TPayload>(signalStoreKey);

            this.internalLogger.LogDebug($"Providing measurement ({metric.Value}) with attributes ({string.Join(",", metric.Attributes.Select(attribute => attribute.Key + ": " + attribute.Value))}) for {signalStoreKey}");

            return new Measurement<TPayload>(
                value: metric.Value,
                tags: metric.Attributes.ToKeyValuePairs()
            );
        }

        /// <summary>
        /// Record a value stored in the signal store via an instrument including alll provided attributes.
        /// </summary>
        /// <typeparam name="TPayload">The type of the value stored.</typeparam>
        /// <param name="signalStoreKey">The key under which the value is stored in the signal store.</param>
        /// <param name="record">An action that will record the payload and the provided attributes to the instrument.</param>
        private void RecordAttributedValue<TPayload>(string signalStoreKey, Action<TPayload, TagList> record) where TPayload : struct
        {
            var metric = this.signalStore.GetValue<TPayload>(signalStoreKey);

            this.internalLogger.LogDebug($"Providing measurement ({metric.Value}) with attributes ({string.Join(",", metric.Attributes.Select(attribute => attribute.Key + ": " + attribute.Value))}) for {signalStoreKey}");

            record(metric.Value, metric.Attributes.ToTagList());
        }

        /// <summary>
        /// Disposes the class and ensures that no signals are sent to the otel servers anymore.
        /// </summary>
        public void Dispose()
        {
            foreach (var meter in this.MeterServerMap.Values)
            {
                meter.Dispose();
            }

            foreach (var meterProvider in this.MeterProviders)
            {
                meterProvider.Dispose();
            }

            foreach (var loggerFactory in this.LoggerFactoryMap.Values)
            {
                loggerFactory.Dispose();
            }
        }
    }
}
