using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using mqtt2otel.Helper;
using mqtt2otel.Interfaces;
using mqtt2otel.InternalMetrics;
using mqtt2otel.Manifest;
using mqtt2otel.ManifestExplorer.DTOs;
using mqtt2otel.Parser;
using mqtt2otel.Stores;
using mqtt2otel.Transformation;
using System.Diagnostics.Metrics;
using System.Text;

namespace mqtt2otel.ManifestExplorer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ManifestExplorerController : ControllerBase
    {
        [HttpPost(nameof(ApplyPatternToPayload))]
        public async Task<ApplyPatternToPayloadResult> ApplyPatternToPayload([FromBody] ApplyPatternToPayloadRequest request)
        {
            ILogger<Processor> logger = new Logger<Processor>(new LoggerFactory());

            IPayloadParser payloadParser = new PayloadParser();
            IPayloadTransformation payloadTransformation = new PayloadTransformation();
            ISignalStore signalStore = new SignalStore();
            ILoggerStore loggerStore = new LoggerStore(payloadParser, payloadTransformation);
            IDataStores dataStores = new DataStores(signalStore, loggerStore);
            ProcessorMeter meter = new ProcessorMeter();

            Manifest.Manifest.ObjectFactory = new mqtt2otel.Manifest.ObjectFactory(logger, payloadParser, payloadTransformation, dataStores, meter);
            var manifest = Manifest.Manifest.ReadFromYaml(logger, yaml: request.Pattern);
            manifest.Validate();
            manifest.Initialize();

            ILogger<MqttCoordinator> mqttLogger = new Logger<MqttCoordinator>(new LoggerFactory());
            var mqtt = new MqttCoordinator(mqttLogger, new MqttMeter(), isSimulator: true);
            await mqtt.ConnectAndSubscribe(manifest);

            ILogger<OtelCoordinator> otelLogger = new Logger<OtelCoordinator>(new LoggerFactory());
            var exportBuilder = new OtelTestExporterBuilder();
            var otel = new OtelCoordinator(otelLogger, exportBuilder, dataStores, new OtelMeter());
            otel.Connect(manifest);

            bool success = await mqtt.ProcessReceivedMessage(request.Topic, request.Payload);

            otel.FlushMeters();
            return new ApplyPatternToPayloadResult(exportBuilder.GetStringRepresentationOfMetrics(), exportBuilder.GetStringRepresentationOfLogEntries());
        }
    }
}
