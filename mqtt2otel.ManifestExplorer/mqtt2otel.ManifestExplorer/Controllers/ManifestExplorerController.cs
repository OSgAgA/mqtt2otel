using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
            ILogger<Processor> logger = new Logger<Processor>(new LoggerFactory());

            IPayloadParser payloadParser = new PayloadParser();
            IPayloadTransformation payloadTransformation = new PayloadTransformation();
            ISignalStore signalStore = new SignalStore();
            ILoggerStore loggerStore = new LoggerStore(payloadParser, payloadTransformation);
            IDataStores dataStores = new DataStores(signalStore, loggerStore);
            ProcessorMeter meter = new ProcessorMeter();

            Manifest.Manifest.ObjectFactory = new mqtt2otel.Manifest.ObjectFactory(logger, payloadParser, payloadTransformation, dataStores, meter);

            Manifest.Manifest manifest;

            try
            {
                manifest = Manifest.Manifest.ReadFromYaml(logger, yaml: request.Pattern);
            }
            catch (YamlException ex)
            {
                var message = $"({ex.Start.ToString()}) - ({ex.End.ToString()}): {ex.Message}";
                return new ApplyPatternToPayloadResult(new ErrorTestData(ex.Message, new Position(ex.Start.Line, ex.Start.Column), new Position(ex.End.Line, ex.End.Column)));
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
            var otel = new OtelCoordinator(otelLogger, exportBuilder, dataStores, new OtelMeter());
            otel.Connect(manifest);

            bool success = await mqtt.ProcessReceivedMessage(request.Topic, request.Payload);

            otel.FlushMeters();
            return new ApplyPatternToPayloadResult(exportBuilder.Metrics.ToList(), exportBuilder.Logs.ToList());
        }
    }
}
