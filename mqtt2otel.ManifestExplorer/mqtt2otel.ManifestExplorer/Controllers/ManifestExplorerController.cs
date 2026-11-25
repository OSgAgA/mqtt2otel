using Microsoft.AspNetCore.Mvc;
using Moq;
using mqtt2otel.Helper;
using mqtt2otel.Interfaces;
using mqtt2otel.InternalMetrics;
using mqtt2otel.Manifest;
using mqtt2otel.ManifestExplorer.DTOs;
using mqtt2otel.ManifestExplorer.Settings;
using mqtt2otel.Metadata;
using mqtt2otel.Parser;
using mqtt2otel.Provider;
using mqtt2otel.Shared;
using mqtt2otel.Stores;
using mqtt2otel.Transformation;
using System.Globalization;
using YamlDotNet.Core;

namespace mqtt2otel.ManifestExplorer.Controllers
{
    /// <summary>
    /// The default controller for the manifest explorer.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class ManifestExplorerController : ControllerBase
    {
        /// <summary>
        /// It seems like the otel debug logger is not fully thread safe. This mutex guards the processor, so that messages are not 
        /// associated to the wrong processor.
        /// </summary>
        public static Mutex processorMutex = new Mutex();

        /// <summary>
        /// A value indicating, whether the application is currently in test mode. If this is the case, the fake data providers should be used instead
        /// of the default ones.
        /// </summary>
        private bool isTestEnvironment = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="ManifestExplorerController"/> class.
        /// </summary>
        /// <param name="appInfo">The application info object.</param>
        public ManifestExplorerController(ApplicationInfo appInfo)
        {
            this.isTestEnvironment = appInfo.IsTestEnvironment;
        }

        /// <summary>
        /// Applies the provided pattern to the provided payload.
        /// </summary>
        /// <param name="request">The request describing the setup.</param>
        /// <returns>The created result.</returns>
        [HttpPost(nameof(ApplyPatternToPayload))]
        public async Task<ApplyPatternToPayloadResult> ApplyPatternToPayload([FromBody] ApplyPatternToPayloadData request)
        {
            var culture = new CultureInfo("en-US");
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;

            List<ErrorTestData> processorErrors = new List<ErrorTestData>();

            Mock<ILogger<Processor>> logger = this.CreateLoggerMock<Processor>(processorErrors);
            Mock<ILogger<OtelCollector>> otelCollectorLogger = this.CreateLoggerMock<OtelCollector>(processorErrors);

            IPayloadParser payloadParser = new PayloadParser();
            IEmbeddedExpressionParser embeddedExpressionParser = new EmbeddedExpressionParser(payloadParser);

            IPayloadTransformation payloadTransformation = new PayloadTransformation();
            ISignalStore signalStore = new SignalStore(embeddedExpressionParser);
            var internalLogger = this.CreateLoggerMock<string>(processorErrors);
            ILoggerStore loggerStore = new LoggerStore(internalLogger.Object, payloadParser, payloadTransformation, embeddedExpressionParser);
            IDataStores dataStores = new DataStores(signalStore, loggerStore);
            ProcessorMeter meter = new ProcessorMeter();
            UsageTelemetryMeter usageMeter = new UsageTelemetryMeter();

            LocalStore traceStore;
            if (request.UseFakeProviders == true || (request.UseFakeProviders == null && this.isTestEnvironment))
            {
                traceStore = new LocalStore(new FakeTraceIdProvider(request.State.TraceIdProviderState));
            }
            else
            {
                traceStore = new LocalStore(new DefaultTraceIdProvider());
            }
            traceStore.Initialize(request.State);

            Manifest.Manifest manifest;

            try
            {
                manifest = Manifest.Manifest.ReadFromYaml(logger.Object, yaml: request.Manifest);
                payloadParser.SetMappings(manifest.Mappings);
                embeddedExpressionParser.SetMappings(manifest.Mappings);
                manifest.WaitForLateSpanEventsInS = 0;
            }
            catch (YamlException ex)
            {
                return new ApplyPatternToPayloadResult(error: new ErrorTestData(ex.Message, new Metadata.Position(ex.Start.Line, ex.Start.Column), new Metadata.Position(ex.End.Line, ex.End.Column)), state: traceStore.GetFullState());
            }
            catch (Exception ex)
            {
                return new ApplyPatternToPayloadResult(error: new ErrorTestData(ex.Message ?? ""), state: traceStore.GetFullState());
            }

            if (manifest.MqttConnections.Count == 0)
            {
                manifest.MqttConnections.Add(new MqttBroker());
            }

            if (manifest.OtelConnections.Count == 0)
            {
                manifest.OtelConnections.Add(new OtelServerConnection()
                {
                    Name = "default-connection",
                    ServiceNamespace = "default-service-namespace",
                    ServiceName = "default-service",
                    ServiceVersion = "1.0.0",
                    Endpoint = new OtelServerEndpoint() { Address = "https://my-otel-endpoint.org" },
                    MinimumLogLevel = LogLevel.Information
                });
            }

            if (string.IsNullOrWhiteSpace(manifest.Version)) manifest.Version = "1.0";

            manifest.Initialize();

            var validationResult = manifest.Validate();

            if (!validationResult.Success)
            {
                return new ApplyPatternToPayloadResult(error: new ErrorTestData(validationResult.ToString() ?? ""), state: traceStore.GetFullState());
            }

            ILogger<MqttCoordinator> mqttLogger = this.CreateLoggerMock<MqttCoordinator>(processorErrors).Object;

            var messageProcessor = new MessageProcessor(CreateLoggerMock<MessageProcessor>(processorErrors).Object, payloadParser, new PayloadTransformation(), dataStores, meter,
                                                        new UsageTelemetryMeter(), embeddedExpressionParser, traceStore, traceStore);
            var mqtt = new MqttCoordinator(mqttLogger, new MqttMeter(), messageProcessor, traceStore, isSimulator: true);
            await mqtt.ConnectAndSubscribe(manifest);

            ILogger<OtelCoordinator> otelLogger = this.CreateLoggerMock<OtelCoordinator>(processorErrors).Object;
            var exportBuilder = new OtelTestExporterBuilder();

            processorMutex.WaitOne(TimeSpan.FromSeconds(30));

            try
            {
                using (var otel = new OtelCoordinator(otelLogger, exportBuilder, dataStores, new OtelInternalMeter(), embeddedExpressionParser, new ApplicationSettings(), new OtelCollector(otelCollectorLogger.Object, isActive: false)))
                {
                    otel.Connect(manifest);

                    var message = new MqttMessage(subscriptionId: 0, topic: request.MqttData.Topic, payload: request.MqttData.Payload, userProperties: request.MqttData.UserProperties, request.MqttData.ResponseTopic, request.MqttData.CorrelationData);
                    bool success = await mqtt.SimulateOnMqttMessageReceived(message);
                    var completedSpans = traceStore.GetCompletedOrTimeOutSpans();
                    otel.CreateSpans(completedSpans);
                    traceStore.RemoveSpans(completedSpans);
                }

                return new ApplyPatternToPayloadResult(exportBuilder, processorErrors, state: traceStore.GetFullState());
            }
            finally
            {
                processorMutex.ReleaseMutex();
            }
        }

        /// <summary>
        /// Creates a mock for an <see cref="ILogger"/> of type T.
        /// </summary>
        /// <typeparam name="T">The logger type.</typeparam>
        /// <param name="processorErrors"></param>
        /// <returns>The created logger.</returns>
        private Mock<ILogger<T>> CreateLoggerMock<T>(List<ErrorTestData>? errorCollector = null)
        {
            if (errorCollector == null) errorCollector = new List<ErrorTestData>();

            var logger = new Mock<ILogger<T>>();
            logger.Setup(x => x.Log(
                                    It.Is<LogLevel>(l => l == LogLevel.Error || l == LogLevel.Critical),
                                    It.IsAny<EventId>(),
                                    It.IsAny<It.IsAnyType>(),
                                    It.IsAny<Exception>(),
                                    (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()))
                                .Callback((LogLevel level, EventId id, object state, Exception ex, object formatter) =>
                                {
                                    if (level == LogLevel.Error || level == LogLevel.Critical)
                                    {
                                        errorCollector.Add(new ErrorTestData(state.ToString() ?? string.Empty));
                                    }
                                });
            return logger;
        }

    }
}
