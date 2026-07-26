using Microsoft.Extensions.Logging;
using mqtt2otel.Helper;
using mqtt2otel.Interfaces;
using mqtt2otel.InternalLogging;
using mqtt2otel.Manifest;
using mqtt2otel.Parser;
using mqtt2otel.Transformation;
using System;
using System.Collections.Generic;
using System.Text;

namespace mqtt2otel.Stores
{
    /// <summary>
    /// Represents a logger for logging mqtt payloads to an open telemetry endpoint.
    /// </summary>
    public class OtelLogger
    {
        /// <summary>
        /// The logger used for logging information to open telemetry.
        /// </summary>
        private readonly ILogger logger;

        /// <summary>
        /// A payload parser for parsing mqtt payloads.
        /// </summary>
        private readonly IPayloadParser payloadParser;

        /// <summary>
        /// A transformation parser for applying transformation to payloads.
        /// </summary>
        private readonly IPayloadTransformation payloadTransformation;

        /// <summary>
        /// The logger used for internal logging.
        /// </summary>
        private readonly ILogger internalLogger;

        /// <summary>
        /// Initializes a new instance of the <see cref="OtelLogger"/> class.
        /// </summary>
        /// <param name="internalLogger">The logger used for internal logging.</param>
        /// <param name="logger">The logger to log to open telemetry.</param>
        /// <param name="payloadParser">The payload parser used for parsing mqtt payloads.</param>
        /// <param name="payloadTransformation">The payload transformation parser used for transforming mqtt payloads.</param>
        public OtelLogger(ILogger internalLogger, ILogger logger, IPayloadParser payloadParser, IPayloadTransformation payloadTransformation)
        {
            this.logger = logger;
            this.payloadParser = payloadParser;
            this.payloadTransformation = payloadTransformation;
            this.internalLogger = internalLogger;
        }

        /// <summary>
        /// Processes a log message given as a string payload.
        /// </summary>
        /// <param name="payload">The payload representing the log message.</param>
        /// <param name="topic">The topic, that triggered the subscription.</param>
        /// <param name="rule">The log rule that define how to interpret the payload.</param>
        /// <param name="variables">Variables that can be applied to the payload.</param>
        /// <param name="internalLogger">The logger used for internal logging.</param>
        /// <param name="combinedAttributes">All attributes that should be applied to the log message.</param>
        /// <returns>A value indicating whether the payload could be processed successfully.</returns>
        public bool ProcessLogMessage(string payload, string topic, OtelLoggingRule rule, IEnumerable<Variable> variables, ILogger internalLogger, IEnumerable<Variable> combinedAttributes)
        {
            if (rule.Name == null) return false;

            if (!string.IsNullOrWhiteSpace(rule.Transform))
            {
                using (this.internalLogger.StartActivity("Logger rule transformation"))
                {
                    payload = this.payloadTransformation.Apply(rule.Name, rule.Transform, new ParsingContext(variables, payload, topic));
                }
            }

            var context = new ParsingContext(variables, payload, topic);
            List<KeyValuePair<string, object?>> attributes = combinedAttributes
                .Select(attribute => new KeyValuePair<string, object?>(
                    EmbeddedExpressionParser.Expand(attribute.Key, context), 
                    EmbeddedExpressionParser.Expand(attribute.Value.ToString() ?? string.Empty, context)))
                .ToList();

            string? body = string.Empty;
            using (this.internalLogger.StartActivity("Logger rule execution"))
            {
                switch (rule.PayloadType)
                {
                    case OtelLoggingPayloadType.Text:
                        body = payload;
                        break;
                    case OtelLoggingPayloadType.Json:
                        var obj = Newtonsoft.Json.Linq.JObject.Parse(payload).ToObject<Dictionary<string, object?>>();

                        if (obj == null) return false;

                        string messageKey = rule.MessageKey;
                        if (obj.ContainsKey(messageKey))
                        {
                            body = obj[messageKey]?.ToString();
                            obj.Remove(messageKey);
                            var additionalAttributes = obj.Select(kvp => new KeyValuePair<string, object?>(kvp.Key, kvp.Value)).ToList();
                            attributes.AddRange(additionalAttributes);
                        }
                        else
                        {
                            body = obj.ToString();
                        }

                        break;
                    default:
                        return false;
                }
            }

            using (this.internalLogger.StartActivity("Log to otel"))
            {
                ApplyLoglevelAndLogToOtel(internalLogger, attributes, body, rule);
            }

            return true;
        }

        /// <summary>
        /// Applies a log level (if provided) and logs the message to open telemetry server.
        /// </summary>
        /// <param name="internalLogger">The logger for internal logging.</param>
        /// <param name="attributes">The log attributes that should be added as a log scope.</param>
        /// <param name="body">The log message body.</param>
        /// <param name="rule">The rule that should be applied for logging.</param>
        private void ApplyLoglevelAndLogToOtel(ILogger internalLogger, List<KeyValuePair<string, object?>> attributes, string? body, OtelLoggingRule rule)
        {
            string loglevelKey = rule.LogLevelKey;

            if (body == null) return;
            using (logger.BeginScope(attributes))
            {
                var attributesDict = attributes.ToDictionary();
                if (attributesDict.ContainsKey(loglevelKey))
                {
                    if (attributesDict[loglevelKey] is string loglevelString && loglevelString != null)
                    {
                        LogLevel loglevel;
                        if (TypeHelper.TryParseLogLevel(loglevelString, out loglevel))
                        {
                            logger.Log(loglevel, body);
                        }
                        else
                        {
                            internalLogger.LogError($"Could not parse {loglevelKey}: '{loglevelString}' as log level.");
                            logger.LogInformation(body);
                        }
                    }
                    else
                    {
                        var obj = attributesDict[loglevelKey]?.ToString();
                        internalLogger.LogError($"Could not parse {loglevelKey}: '{obj}' of type {obj?.GetType().FullName} as log level.");
                        logger.LogInformation(body);
                    }
                }
                else
                {
                    logger.LogInformation(body);
                }
            }
        }
    }
}
