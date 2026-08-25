using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using mqtt2otel.Helper;
using mqtt2otel.InternalMetrics;
using mqtt2otel.Parser;
using mqtt2otel.Stores;
using mqtt2otel.Transformation;
using System;
using System.Collections.Generic;
using System.Text;

namespace mqtt2otel.Tests.Helper
{
    public static class ManifestHelper
    {
        public static Manifest.Manifest ReadManifestFromString(string yaml, DataStores? dataStores = null)
        {
            var objFactoryLoggerMock = new Mock<ILogger<Manifest.Processor>>();
            var internalLoggerMock = new Mock<ILogger<string>>();

            var payloadParser = new PayloadParser();
            var embeddedExpressionParser = new EmbeddedExpressionParser(payloadParser);

            if (dataStores == null)
            {
                var signalStore = new SignalStore(embeddedExpressionParser);
                var loggerStore = new LoggerStore(internalLoggerMock.Object, payloadParser, new PayloadTransformation(), embeddedExpressionParser);

                dataStores = new DataStores(signalStore, loggerStore);
            }

            Manifest.Manifest.ObjectFactory = new Manifest.ObjectFactory(objFactoryLoggerMock.Object, payloadParser, new PayloadTransformation(), dataStores, new ProcessorMeter(), embeddedExpressionParser);

            var loggerMock = new Mock<ILogger>();

            var manifest = Manifest.Manifest.ReadFromYaml(loggerMock.Object, yaml: yaml);

            return manifest;
        }

    }
}
