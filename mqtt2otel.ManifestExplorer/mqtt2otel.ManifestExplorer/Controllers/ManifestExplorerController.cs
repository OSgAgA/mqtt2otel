using Microsoft.AspNetCore.Mvc;
using Moq;
using mqtt2otel.Helper;
using mqtt2otel.Interfaces;
using mqtt2otel.InternalMetrics;
using mqtt2otel.Manifest;
using mqtt2otel.ManifestExplorer.DTOs;
using mqtt2otel.Metadata;
using mqtt2otel.Parser;
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

            var logger = new Mock<ILogger<Processor>>();
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
                                        processorErrors.Add(new ErrorTestData(state.ToString() ?? string.Empty));
                                    }
                                });

            IPayloadParser payloadParser = new PayloadParser();
            IEmbeddedExpressionParser embeddedExpressionParser = new EmbeddedExpressionParser(payloadParser);

            IPayloadTransformation payloadTransformation = new PayloadTransformation();
            ISignalStore signalStore = new SignalStore(embeddedExpressionParser);
            var internalLogger = new Mock<ILogger<string>>();
            ILoggerStore loggerStore = new LoggerStore(internalLogger.Object, payloadParser, payloadTransformation, embeddedExpressionParser);
            IDataStores dataStores = new DataStores(signalStore, loggerStore);
            ProcessorMeter meter = new ProcessorMeter();
            UsageTelemetryMeter usageMeter = new UsageTelemetryMeter();
            LocalTraceStore traceStore = new LocalTraceStore();
            traceStore.Initialize(request.State);

            var objectFactory = new mqtt2otel.Manifest.ObjectFactory(logger.Object, payloadParser, payloadTransformation, dataStores, meter, usageMeter, embeddedExpressionParser, traceStore);

            Manifest.Manifest manifest;

            try
            {
                manifest = Manifest.Manifest.ReadFromYaml(objectFactory, logger.Object, yaml: request.Manifest);
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

            ILogger<MqttCoordinator> mqttLogger = new Logger<MqttCoordinator>(new LoggerFactory());
            var mqtt = new MqttCoordinator(mqttLogger, new MqttMeter(), isSimulator: true);
            await mqtt.ConnectAndSubscribe(manifest);

            ILogger<OtelCoordinator> otelLogger = new Logger<OtelCoordinator>(new LoggerFactory());
            var exportBuilder = new OtelTestExporterBuilder();

            processorMutex.WaitOne(TimeSpan.FromSeconds(30));

            try
            {
                using (var otel = new OtelCoordinator(otelLogger, exportBuilder, dataStores, new OtelInternalMeter(), embeddedExpressionParser, new ApplicationSettings()))
                {
                    otel.Connect(manifest);

                    var message = new MqttMessage(subscriptionId: 0, topic: request.MqttData.Topic, payload: request.MqttData.Payload, userProperties: request.MqttData.UserProperties);
                    bool success = await mqtt.SimulateOnMqttMessageReceived(message);
                    otel.CreateSpans(traceStore.GetCompletedSpans());
                }

                return new ApplyPatternToPayloadResult(exportBuilder, processorErrors, state: traceStore.GetFullState());
            }
            finally
            {
                processorMutex.ReleaseMutex();
            }
        }
    }
}
