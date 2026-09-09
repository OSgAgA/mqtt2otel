using BlazorBootstrap;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
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
using System.Diagnostics.Metrics;
using System.Globalization;
using System.Text;
using System.Text.Json;
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

            Manifest.Manifest.ObjectFactory = new mqtt2otel.Manifest.ObjectFactory(logger.Object, payloadParser, payloadTransformation, dataStores, meter, embeddedExpressionParser);

            Manifest.Manifest manifest;

            try
            {
                manifest = Manifest.Manifest.ReadFromYaml(logger.Object, yaml: request.Pattern);
            }
            catch (YamlException ex)
            {
                return new ApplyPatternToPayloadResult(new ErrorTestData(ex.Message, new Metadata.Position(ex.Start.Line, ex.Start.Column), new Metadata.Position(ex.End.Line, ex.End.Column)));
            }
            catch (Exception ex)
            {
                return new ApplyPatternToPayloadResult(new ErrorTestData(ex.Message ?? ""));

            }

            if (manifest.MqttConnections.Count == 0)
            {
                manifest.MqttConnections.Add(new MqttBroker());
            }

            if (manifest.OtelConnections.Count == 0)
            {
                manifest.OtelConnections.Add(new OtelServerConnection());
            }

            if (string.IsNullOrWhiteSpace(manifest.Version)) manifest.Version = "1.0";

            manifest.Initialize();

            var validationResult = manifest.Validate();

            if (!validationResult.Success)
            {
                return new ApplyPatternToPayloadResult(new ErrorTestData(validationResult.ToString() ?? ""));
            }

            ILogger<MqttCoordinator> mqttLogger = new Logger<MqttCoordinator>(new LoggerFactory());
            var mqtt = new MqttCoordinator(mqttLogger, new MqttMeter(), isSimulator: true);
            await mqtt.ConnectAndSubscribe(manifest);

            ILogger<OtelCoordinator> otelLogger = new Logger<OtelCoordinator>(new LoggerFactory());
            var exportBuilder = new OtelTestExporterBuilder();
            var otel = new OtelCoordinator(otelLogger, exportBuilder, dataStores, new OtelMeter(), embeddedExpressionParser);
            otel.Connect(manifest);

            var message = new MqttMessage(subscriptionId: 0, topic: request.Topic, payload: request.Payload, userProperties: request.UserProperties);
            bool success = await mqtt.SimulateOnMqttMessageReceived(message);

            otel.FlushMeters();

            if (processorErrors.Any())
            {
                return new ApplyPatternToPayloadResult(processorErrors);
            }

            return new ApplyPatternToPayloadResult(exportBuilder.Metrics.ToList(), exportBuilder.Logs.ToList());
        }
    }
}
