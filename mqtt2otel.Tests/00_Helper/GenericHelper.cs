using Microsoft.Extensions.Logging;
using Moq;
using mqtt2otel.Helper;
using mqtt2otel.Interfaces;
using mqtt2otel.Manifest;
using mqtt2otel.Parser;
using mqtt2otel.Stores;
using mqtt2otel.Transformation;
using System;
using System.Collections.Generic;
using System.Text;

namespace mqtt2otel.Tests.Helper
{
    public static class GenericHelper
    {
        /// <summary>
        /// Gets an empty <see cref="DataStores"/> object.
        /// </summary>
        /// <returns>The created data stores.</returns>
        public static DataStores GetDataStores(PayloadParser? payloadParser, EmbeddedExpressionParser? embeddedExpressionParser)
        {
            if (payloadParser == null)
            {
                payloadParser = new PayloadParser();
            }

            if (embeddedExpressionParser == null)
            {
                embeddedExpressionParser = new EmbeddedExpressionParser(payloadParser);
            }

            var signalStore = new SignalStore(embeddedExpressionParser);
            var internalLogger = new Mock<ILogger<string>>();
            var loggerStore = new LoggerStore(internalLogger.Object, payloadParser, new PayloadTransformation(), embeddedExpressionParser);

            return new DataStores(signalStore, loggerStore);
        }
    }
}
