using Microsoft.Extensions.Logging;
using Moq;
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
        public static Manifest.Manifest ReadManifestFromString(string yaml)
        {
            var objFactoryLoggerMock = new Mock<ILogger<Manifest.Processor>>();
            var signalStore = new SignalStore();
            var loggerStore = new LoggerStore(new PayloadParser(), new PayloadTransformation());

            var dataStores = new DataStores(signalStore, loggerStore);
            Manifest.Manifest.ObjectFactory = new Manifest.ObjectFactory(objFactoryLoggerMock.Object, new PayloadParser(), new PayloadTransformation(), dataStores);

            var loggerMock = new Mock<ILogger>();

            return Manifest.Manifest.ReadFromYaml(loggerMock.Object, yaml: yaml);
        }

    }
}
