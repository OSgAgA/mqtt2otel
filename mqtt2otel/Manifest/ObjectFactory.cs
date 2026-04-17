using Microsoft.Extensions.Logging;
using mqtt2otel.Parser;
using mqtt2otel.Stores;
using mqtt2otel.Transformation;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.ObjectFactories;

namespace mqtt2otel.Manifest
{
    /// <summary>
    /// Creates object for the yaml parser.
    /// </summary>
    public class ObjectFactory : IObjectFactory
    {
        /// <summary>
        /// The default fallback factory used, for all cases that are not explicitly handled via this class.
        /// </summary>
        private readonly IObjectFactory fallback;

        /// <summary>
        /// The data stores used by the application to exchange data asynchronously.
        /// </summary>
        private DataStores dataStores;

        /// <summary>
        /// The logger used internaly for logging.
        /// </summary>
        private ILogger internalLogger;

        /// <summary>
        /// The payload parser for processing payloads.
        /// </summary>
        private PayloadParser payloadParser;

        /// <summary>
        /// The object used for processing payload transformations.
        /// </summary>
        private PayloadTransformation payloadTransformation;

        /// <summary>
        /// Creates a new instance of the <see cref="ObjectFactory"/> class.
        /// </summary>
        /// <param name="signalStore">The store used for storing data for otel signals.</param>
        /// <param name="internalLogger">The logger used internaly for logging.</param>
        /// <param name="payloadParser">The payload parser for processing payloads.</param>
        /// <param name="payloadTransformation">The object used for processing payload transformations.</param>
        /// <param name="dataStores">The data stores used by the application to exchange data asynchronously.</param>
        public ObjectFactory(ILogger<Processor> internalLogger, PayloadParser payloadParser, PayloadTransformation payloadTransformation, DataStores dataStores)
        {
            fallback = new DefaultObjectFactory();
            this.internalLogger = internalLogger;
            this.payloadParser = payloadParser;
            this.payloadTransformation = payloadTransformation;
            this.dataStores = dataStores;
        }

        /// <summary>
        /// Ensure that the <see cref="Processor"/> object is initialized.
        /// </summary>
        /// <param name="type">The type to be created.</param>
        /// <returns>The created object.</returns>
        public object Create(Type type)
        {
            if (type == typeof(Processor))
            {
                return new Processor(this.internalLogger, this.payloadParser, this.payloadTransformation, this.dataStores);
            }

            return fallback.Create(type);
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public object CreatePrimitive(Type type) => fallback.CreatePrimitive(type) ?? 0;

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public bool GetDictionary(IObjectDescriptor descriptor, out IDictionary? dictionary, out Type[]? genericArguments)
            => fallback.GetDictionary(descriptor, out dictionary, out genericArguments);

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public Type GetValueType(Type type) => fallback.GetValueType(type);

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public void ExecuteOnDeserializing(object value) => fallback.ExecuteOnDeserializing(value);

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public void ExecuteOnDeserialized(object value) => fallback.ExecuteOnDeserialized(value);

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public void ExecuteOnSerializing(object value) => fallback.ExecuteOnSerializing(value);

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public void ExecuteOnSerialized(object value) => fallback.ExecuteOnSerialized(value);
    }
}
