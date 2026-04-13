using Microsoft.Extensions.Logging;
using mqtt2otel.Configuration;
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
        /// <param name="loggingSettings">The log settings that define how to interpret the payload.</param>
        /// <param name="variables">Variables that can be applied to the payload.</param>
        /// <param name="internalLogger">The logger used for internal logging.</param>
        /// <returns>A value indicating whether the payload could be processed successfully.</returns>
        public async Task<bool> ProcessLogMessage(string payload, LoggingRuleSettings loggingSettings, IEnumerable<Variable> variables, ILogger internalLogger)
        {
            foreach (var otelSettings in loggingSettings.Otel.Rules)
            {
                if (otelSettings.Name == null) return false;

                if (!string.IsNullOrWhiteSpace(otelSettings.Transform))
                {
                    payload = await this.payloadTransformation.Apply(SubscriptionType.Log, otelSettings.Name, payload, otelSettings.Transform);
                }

                var combinedAttributes = otelSettings.Attributes.Combine(loggingSettings.Otel.Attributes);

                List<KeyValuePair<string, object?>> attributes = combinedAttributes
                    .Select(attribute => new KeyValuePair<string, object?>(attribute.Key, VariableParser.Expand(attribute.Value.ToString() ?? string.Empty, variables)))
                    .ToList();

                string? body = string.Empty;

                switch (otelSettings.PayloadType)
                {
                    case OtelLoggingPayloadType.Text:
                        body = await this.payloadParser.Parse<string>(SubscriptionType.Log, loggingSettings.Name, payload, otelSettings.Filter);
                        break;
                    case OtelLoggingPayloadType.Json:
                        var obj = Newtonsoft.Json.Linq.JObject.Parse(payload).ToObject<Dictionary<string, object?>>();

                        if (obj == null) return false;

                        string messageKey = loggingSettings.MessageKey;
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

                ApplyLoglevelAndLogToOtel(internalLogger, attributes, body, loggingSettings);
            }

            return true;
        }

        /// <summary>
        /// Applies a log level (if provided) and logs the message to open telemetry server.
        /// </summary>
        /// <param name="internalLogger">The logger for internal logging.</param>
        /// <param name="attributes">The log attributes that should be added as a log scope.</param>
        /// <param name="body">The log message body.</param>
        /// <param name="ruleSettings">The settings identifying the rule that should be applied for logging.</param>
        private void ApplyLoglevelAndLogToOtel(ILogger internalLogger, List<KeyValuePair<string, object?>> attributes, string? body, LoggingRuleSettings ruleSettings)
        {
            string loglevelKey = ruleSettings.LogLevelKey;

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
