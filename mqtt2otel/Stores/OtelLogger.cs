using Microsoft.Extensions.Logging;
using mqtt2otel.Manifest;
using mqtt2otel.Helper;
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
        private readonly PayloadParser payloadParser;

        /// <summary>
        /// A transformation parser for applying transformation to payloads.
        /// </summary>
        private readonly PayloadTransformation payloadTransformation;

        /// <summary>
        /// Initializes a new instance of the <see cref="OtelLogger"/> class.
        /// </summary>
        /// <param name="logger">The logger to log to open telemetry.</param>
        /// <param name="payloadParser">The payload parser used for parsing mqtt payloads.</param>
        public OtelLogger(ILogger logger, PayloadParser payloadParser)
        {
            this.logger = logger;
            this.payloadParser = payloadParser;
            this.payloadTransformation = new PayloadTransformation();
        }

        /// <summary>
        /// Processes a log message given as a string payload.
        /// </summary>
        /// <param name="payload">The payload representing the log message.</param>
        /// <param name="rule">The log rule that define how to interpret the payload.</param>
        /// <param name="variables">Variables that can be applied to the payload.</param>
        /// <param name="internalLogger">The logger used for internal logging.</param>
        /// <param name="combinedAttributes">All attributes that should be applied to the log message.</param>
        /// <returns>A value indicating whether the payload could be processed successfully.</returns>
        public async Task<bool> ProcessLogMessage(string payload, OtelLoggingRule rule, IEnumerable<Variable> variables, ILogger internalLogger, IEnumerable<Variable> combinedAttributes)
        {
            if (rule.Name == null) return false;

            if (!string.IsNullOrWhiteSpace(rule.Transform))
            {
                payload = await this.payloadTransformation.Apply(rule.Name, payload, rule.Transform);
            }

            List<KeyValuePair<string, object?>> attributes = combinedAttributes
                .Select(attribute => new KeyValuePair<string, object?>(attribute.Key, VariableParser.Expand(attribute.Value.ToString() ?? string.Empty, variables)))
                .ToList();

            string? body = string.Empty;

            switch (rule.PayloadType)
            {
                case OtelLoggingPayloadType.Text:
                    body = await this.payloadParser.Parse<string>(rule.Name, payload, rule.Filter);
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
                        body = string.Format("{payload}", obj);
                    }

                    break;
                default:
                    return false;
            }

            ApplyLoglevelAndLogToOtel(internalLogger, attributes, body, rule);

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
