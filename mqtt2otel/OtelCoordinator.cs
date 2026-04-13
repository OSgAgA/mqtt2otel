using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using mqtt2otel.Configuration;
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
        /// <param name="settings">The settings.</param>
        /// <param name="signalStore">The signal store to store metric signals.</param>
        /// <param name="loggerStore">The store for storing loggers created by this instance.</param>
        public OtelCoordinator(ILogger<OtelCoordinator> internalLogger, Manifest settings, SignalStore signalStore, LoggerStore loggerStore)
        {
            this.internalLogger = internalLogger;
            this.signalStore = signalStore;
            this.loggerStore = loggerStore;

            foreach (var otelServer in settings.OtelServer)
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
            this.InitializeMeters(settings);
            this.internalLogger.LogInformation("Otel meters initialization successful.");

            this.internalLogger.LogInformation("Initializing otel logging...");
            this.InitializeLogging(settings);
            this.internalLogger.LogInformation("Otel logging initialization successful.");

        }

        /// <summary>
        /// Initializes the otel meters based on the provided settings.
        /// </summary>
        /// <param name="settings">The rules for creating meters.</param>
        public void InitializeMeters(Manifest settings)
        {

            // Create a separate meter for each server.
            foreach (var otelServerSettings in settings.OtelServer)
            {
                var meter = new Meter(otelServerSettings.Name);

                this.MeterServerMap[otelServerSettings.Name] = meter;
            }

            // Create all instruments
            foreach (var metricSettings in settings.Metrics)
            {
                foreach (var subscriptionSettings in metricSettings.Mqtt.Subscriptions)
                {
                    this.CreateInstruments(metricSettings, subscriptionSettings);
                }
            }

            // Create meter providers
            foreach (var otelServerSettings in settings.OtelServer)
            {
                var provider = Sdk.CreateMeterProviderBuilder()
                    .SetResourceBuilder(
                            ResourceBuilder.CreateDefault()
                            .AddService(otelServerSettings.ServiceName, serviceNamespace: otelServerSettings.ServiceNamespace))
                    .AddOtlpExporter(otlpOptions => this.InitializeExporterOptions(otlpOptions, otelServerSettings))
                    .AddMeter(otelServerSettings.Name)
                    .Build();

                this.MeterProviders.Add(provider);
            }
        }

        /// <summary>
        /// Initializes <see cref="OtlpExporterOptions"/> based on the provided settings.
        /// </summary>
        /// <param name="otlpOptions">The options that will be initialized.</param>
        /// <param name="serverSettings">The settings defining the options to be applied.</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private OtlpExporterOptions InitializeExporterOptions(OtlpExporterOptions otlpOptions, OtelServerSettings serverSettings)
        {
            if (serverSettings.Endpoint.Address == null) throw new Exception("Address of Otel server endpoint must be set!");

            otlpOptions.Endpoint = serverSettings.Endpoint.Uri;
            otlpOptions.Protocol = serverSettings.OtlpExportProtocol;
            otlpOptions.ExportProcessorType = serverSettings.ExportProcessorType;

            if(serverSettings.Endpoint.Headers != null)
            {
                otlpOptions.Headers = serverSettings.Endpoint.Headers;
            }

            if (serverSettings.Endpoint.BatchTimeoutInMs != null)
            {
                otlpOptions.TimeoutMilliseconds = serverSettings.Endpoint.BatchTimeoutInMs.Value;
            }

            if (serverSettings.ClientPrefix != null)
            {
                otlpOptions.UserAgentProductIdentifier = serverSettings.ClientPrefix;
            }

            if (serverSettings.Endpoint.EnableTls)
            {
                otlpOptions.HttpClientFactory = () =>
                {
                    var handler = new HttpClientHandler();
                    var cert = X509CertificateLoader.LoadPkcs12FromFile(serverSettings.Endpoint.ClientCertificatePath, serverSettings.Endpoint.ClientCertificatePassword);

                    handler.ClientCertificates.Add(cert);
                    return new HttpClient(handler);
                };
            }

            return otlpOptions;
        }

        /// <summary>
        /// Create and store all otel meter instruments based on the given rules.
        /// </summary>
        /// <param name="metricSettings">The meter rule settings defining how the meter is created.</param>
        /// <param name="mqttSubscriptionSettings">The mqtt subscription settings defining the subscription with which the meter is connected.</param>
        /// <exception cref="ArgumentNullException">Thrown if a subscription settings rule is defined without a name.</exception>
        /// <exception cref="Exception">Thrown if instrument does not exist.</exception>
        private void CreateInstruments(MetricsRuleSettings metricSettings, MqttSubscriptionSettings mqttSubscriptionSettings)
        {
            foreach (var rulesSettings in metricSettings.Otel.Rules)
            {
                if (rulesSettings.Name == null) throw new ArgumentNullException(nameof(rulesSettings));

                if (rulesSettings.OtelServerName == null || !this.MeterServerMap.ContainsKey(rulesSettings.OtelServerName))
                {
                    throw new Exception($"Cannot create instrument. No meter exists for metric rule with name: {rulesSettings.Name}.");
                }

                string expandedName = VariableParser.Expand(rulesSettings.Name, mqttSubscriptionSettings.Variables);

                string key = mqttSubscriptionSettings.Id + ":" + rulesSettings.Id;

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
                        throw new Exception($"Unsupported otel metric type: '{rulesSettings.Instrument.ToString()}' for metric {metricSettings.Name}.");
                }

                var meter = this.MeterServerMap[rulesSettings.OtelServerName];
                TypeHelper.CallMethodWithGenericType(this, rulesSettings.SignalDataType, instrumentCreationMethodName, new object[] { rulesSettings, mqttSubscriptionSettings, key, meter, expandedName });
            }
        }

        /// <summary>
        /// Initializes open telemetry logging based on the provided settings.
        /// </summary>
        /// <param name="settings">The settings providing the logging configuration and the logging rules.</param>
        /// <exception cref="Exception">Thrown if no otel server endpoint is defined.</exception>
        private void InitializeLogging(Manifest settings)
        {
            // Create log factories for each server

            foreach (var otelServerSettings in settings.OtelServer)
            {
                this.LoggerFactoryMap[otelServerSettings.Name] = Microsoft.Extensions.Logging.LoggerFactory.Create(builder =>
                {
                    builder.AddOpenTelemetry(options =>
                    {
                        options.SetResourceBuilder(
                            ResourceBuilder.CreateDefault()
                            .AddService(otelServerSettings.ServiceName, serviceNamespace: otelServerSettings.ServiceNamespace))
                            .IncludeScopes = true;

                        options.AddOtlpExporter(otlpOptions => this.InitializeExporterOptions(otlpOptions, otelServerSettings));

                        options.AddProcessor(new TimestampOverrideProcessor());
                    });
                });
            }

            // Prrocess logging rules

            foreach (var loggingSettings in settings.Logs)
            {
                foreach (var otelLoggingRuleSettings in loggingSettings.Otel.Rules)
                {
                    if (otelLoggingRuleSettings.OtelServerName == null) throw new Exception($"Internal error: OtelServerName must not be null for logging rule: {otelLoggingRuleSettings.Name}");

                    var logger = this.LoggerFactoryMap[otelLoggingRuleSettings.OtelServerName].CreateLogger(otelLoggingRuleSettings.CategoryName);
                    this.loggerStore.StoreLogger(otelLoggingRuleSettings.Id, logger);
                }
            }
        }

        /// <summary>
        /// Creates an asynchronous gauge instrument and the corresponding signal store entry.
        /// </summary>
        /// <typeparam name="T">The type of the signal stored.</typeparam>
        /// <param name="otelMetricRuleSettings">The rule settings defining the metric.</param>
        /// <param name="mqttSubscriptionSettings">The subscription settings for connecting the instrument with the subscription.</param>
        /// <param name="key">The key under which the value will be stored in the signal store.</param>
        /// <param name="meter">The meter to which this instrument should be added.</param>
        private void CreateAsynchronousGauge<T>(OtelMetricRuleSettings otelMetricRuleSettings, MqttSubscriptionSettings mqttSubscriptionSettings, string key, Meter meter, string expandedName) where T : struct
        {
            this.signalStore.StoreValue<T>(key, new OtelMetric<T>(default(T), description: otelMetricRuleSettings.Description, otelMetricRuleSettings.Unit, new List<Variable>()));
            meter.CreateObservableGauge<T>(
                expandedName,
                () => this.CreateMeasurement<T>(key),
                unit: otelMetricRuleSettings.Unit,
                description: otelMetricRuleSettings.Description);
        }

        /// <summary>
        /// Creates a synchronous gauge instrument and the corresponding signal store entry.
        /// </summary>
        /// <typeparam name="T">The type of the signal stored.</typeparam>
        /// <param name="otelMetricRuleSettings">The rule settings defining the metric.</param>
        /// <param name="mqttSubscriptionSettings">The subscription settings for connecting the instrument with the subscription.</param>
        /// <param name="expandedName">The name that will identify the gauge. The name should already have all variables expanded.</param>
        /// <param name="key">The key under which the value will be stored in the signal store.</param>
        /// <param name="meter">The meter to which this instrument should be added.</param>
        private void CreateGauge<T>(OtelMetricRuleSettings otelMetricRuleSettings, MqttSubscriptionSettings mqttSubscriptionSettings, string key, Meter meter, string expandedName) where T : struct
        {
            this.signalStore.StoreValue<T>(key, new OtelMetric<T>(default(T), description: otelMetricRuleSettings.Description, otelMetricRuleSettings.Unit, new List<Variable>()));
            var gauge = meter.CreateGauge<T>(
                expandedName,
                unit: otelMetricRuleSettings.Unit,
                description: otelMetricRuleSettings.Description);
            this.signalStore.RegisterCallback(key, signalStoreKey => this.RecordAttributedValue<T>(signalStoreKey, (value, attributes) => gauge.Record(value, attributes)));
        }

        /// <summary>
        /// Creates an asynchronous counter instrument and the corresponding signal store entry.
        /// </summary>
        /// <typeparam name="T">The type of the signal stored.</typeparam>
        /// <param name="otelMetricRuleSettings">The rule settings defining the metric.</param>
        /// <param name="mqttSubscriptionSettings">The subscription settings for connecting the instrument with the subscription.</param>
        /// <param name="key">The key under which the value will be stored in the signal store.</param>
        /// <param name="meter">The meter to which this instrument should be added.</param>
        private void CreateAsynchronousCounter<T>(OtelMetricRuleSettings otelMetricRuleSettings, MqttSubscriptionSettings mqttSubscriptionSettings, string key, Meter meter, string expandedName) where T : struct
        {
            this.signalStore.StoreValue<T>(key, new OtelMetric<T>(default(T), description: otelMetricRuleSettings.Description, otelMetricRuleSettings.Unit, new List<Variable>()));
            meter.CreateObservableCounter<T>(
                expandedName,
                () => this.CreateMeasurement<T>(key),
                unit: otelMetricRuleSettings.Unit,
                description: otelMetricRuleSettings.Description);
        }

        /// <summary>
        /// Creates an synchronous counter instrument and the corresponding signal store entry.
        /// </summary>
        /// <typeparam name="T">The type of the signal stored.</typeparam>
        /// <param name="otelMetricRuleSettings">The rule settings defining the metric.</param>
        /// <param name="mqttSubscriptionSettings">The subscription settings for connecting the instrument with the subscription.</param>
        /// <param name="key">The key under which the value will be stored in the signal store.</param>
        /// <param name="meter">The meter to which this instrument should be added.</param>
        private void CreateCounter<T>(OtelMetricRuleSettings otelMetricRuleSettings, MqttSubscriptionSettings mqttSubscriptionSettings, string key, Meter meter, string expandedName) where T : struct
        {
            this.signalStore.StoreValue<T>(key, new OtelMetric<T>(default(T), description: otelMetricRuleSettings.Description, otelMetricRuleSettings.Unit, new List<Variable>()));
            var counter = meter.CreateCounter<T>(
                expandedName,
                unit: otelMetricRuleSettings.Unit,
                description: otelMetricRuleSettings.Description);
            this.signalStore.RegisterCallback(key, signalStoreKey => this.RecordAttributedValue<T>(signalStoreKey, (value, attributes) => counter.Add(value, attributes)));
        }

        /// <summary>
        /// Creates an asynchronous UpDownCounter instrument and the corresponding signal store entry.
        /// </summary>
        /// <typeparam name="T">The type of the signal stored.</typeparam>
        /// <param name="otelMetricRuleSettings">The rule settings defining the metric.</param>
        /// <param name="mqttSubscriptionSettings">The subscription settings for connecting the instrument with the subscription.</param>
        /// <param name="key">The key under which the value will be stored in the signal store.</param>
        /// <param name="meter">The meter to which this instrument should be added.</param>
        private void CreateAsynchronousUpDownCounter<T>(OtelMetricRuleSettings otelMetricRuleSettings, MqttSubscriptionSettings mqttSubscriptionSettings, string key, Meter meter, string expandedName) where T : struct
        {
            this.signalStore.StoreValue<T>(key, new OtelMetric<T>(default(T), description: otelMetricRuleSettings.Description, otelMetricRuleSettings.Unit, new List<Variable>()));
            meter.CreateObservableUpDownCounter<T>(
                expandedName,
                () => this.CreateMeasurement<T>(key),
                unit: otelMetricRuleSettings.Unit,
                description: otelMetricRuleSettings.Description);
        }

        /// <summary>
        /// Creates a synchronous UpDownCounter instrument and the corresponding signal store entry.
        /// </summary>
        /// <typeparam name="T">The type of the signal stored.</typeparam>
        /// <param name="otelMetricRuleSettings">The rule settings defining the metric.</param>
        /// <param name="mqttSubscriptionSettings">The subscription settings for connecting the instrument with the subscription.</param>
        /// <param name="key">The key under which the value will be stored in the signal store.</param>
        /// <param name="meter">The meter to which this instrument should be added.</param>
        private void CreateUpDownCounter<T>(OtelMetricRuleSettings otelMetricRuleSettings, MqttSubscriptionSettings mqttSubscriptionSettings, string key, Meter meter, string expandedName) where T : struct
        {
            this.signalStore.StoreValue<T>(key, new OtelMetric<T>(default(T), description: otelMetricRuleSettings.Description, otelMetricRuleSettings.Unit, new List<Variable>()));
            var counter = meter.CreateUpDownCounter<T>(
                expandedName,
                unit: otelMetricRuleSettings.Unit,
                description: otelMetricRuleSettings.Description);
            this.signalStore.RegisterCallback(key, signalStoreKey => this.RecordAttributedValue<T>(signalStoreKey, (value, attributes) => counter.Add(value, attributes)));
        }

        /// <summary>
        /// Creates a synchronous histogram instrument and the corresponding signal store entry.
        /// </summary>
        /// <typeparam name="T">The type of the signal stored.</typeparam>
        /// <param name="otelMetricRuleSettings">The rule settings defining the metric.</param>
        /// <param name="mqttSubscriptionSettings">The subscription settings for connecting the instrument with the subscription.</param>
        /// <param name="key">The key under which the value will be stored in the signal store.</param>
        /// <param name="meter">The meter to which this instrument should be added.</param>
        private void CreateHistogram<T>(OtelMetricRuleSettings otelMetricRuleSettings, MqttSubscriptionSettings mqttSubscriptionSettings, string key, Meter meter, string expandedName) where T : struct
        {
            this.signalStore.StoreValue<T>(key, new OtelMetric<T>(default(T), description: otelMetricRuleSettings.Description, otelMetricRuleSettings.Unit, new List<Variable>()));

            Histogram<T>? histogram = null;

            if (otelMetricRuleSettings.HistogramBucketBoundaries != null && otelMetricRuleSettings.HistogramBucketBoundaries.Count > 0)
            {
                var list = TypeHelper.Parse<T>(otelMetricRuleSettings.HistogramBucketBoundaries);
                var readonlyList = list.AsReadOnly<T>();
                var advice = new InstrumentAdvice<T>() { HistogramBucketBoundaries = readonlyList };
                histogram = meter.CreateHistogram<T>(
                    expandedName,
                    unit: otelMetricRuleSettings.Unit,
                    description: otelMetricRuleSettings.Description,
                    advice: advice);
            }
            else
            {
                histogram = meter.CreateHistogram<T>(
                    expandedName,
                    unit: otelMetricRuleSettings.Unit,
                    description: otelMetricRuleSettings.Description);
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
