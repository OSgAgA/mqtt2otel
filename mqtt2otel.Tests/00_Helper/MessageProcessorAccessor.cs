using Microsoft.Extensions.Logging;
using Moq;
using mqtt2otel.Helper;
using mqtt2otel.Interfaces;
using mqtt2otel.InternalMetrics;
using mqtt2otel.Parser;
using mqtt2otel.Stores;
using mqtt2otel.Transformation;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace mqtt2otel.Tests.Helper
{
    /// <summary>
    /// A static helper class for accessing a message processor.
    /// </summary>
    public static class MessageProcessorAccessor
    {
        /// <summary>
        /// Creates a new message processor.
        /// </summary>
        /// <param name="payloadParser">The payload parser.</param>
        /// <param name="embeddedExpressionParser">The embedded expression parser.</param>
        /// <param name="dataStores">The data stores.</param>
        /// <param name="traceStore">The trace store.</param>
        /// <param name="meter">The meter for getting internal metrics.</param>
        /// <param name="usageTelemetryMeter">The usage telemetry meter.</param>
        /// <param name="internalLogger">The internal logger.</param>
        /// <param name="payloadTransformation">The payload transformation.</param>
        /// <returns>The created MessageProcessor.</returns>
        public static MessageProcessor CreateNew(IPayloadParser payloadParser, IEmbeddedExpressionParser embeddedExpressionParser, IDataStores dataStores, ITraceStore? traceStore = null,
                                                 ProcessorMeter? meter = null, UsageTelemetryMeter? usageTelemetryMeter = null, ILogger<MessageProcessor>? internalLogger = null, IPayloadTransformation? payloadTransformation = null)
        {
            if (meter == null) meter = new ProcessorMeter();
            if (usageTelemetryMeter == null) usageTelemetryMeter = new UsageTelemetryMeter();
            if (internalLogger == null) internalLogger = new Mock<ILogger<MessageProcessor>>().Object;
            if (payloadTransformation == null) payloadTransformation = new PayloadTransformation();
            if (traceStore == null) traceStore = new LocalTraceStore();

            return new MessageProcessor(internalLogger, payloadParser, payloadTransformation, dataStores, meter, usageTelemetryMeter, embeddedExpressionParser, traceStore);
        }
    }
}
