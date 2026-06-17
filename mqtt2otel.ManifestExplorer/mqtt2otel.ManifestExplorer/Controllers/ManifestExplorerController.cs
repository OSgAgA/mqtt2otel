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
    [Route("api/[controller]")]
    [ApiController]
    public class ManifestExplorerController : ControllerBase
    {
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

        [HttpPost(nameof(CreateJson))]
        public async Task<string> CreateJson([FromBody] ApplyPatternToPayloadData request)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            string result = JsonSerializer.Serialize(this.ApplyPatternToPayload(request), options);

            return result;
        }
    }
}
