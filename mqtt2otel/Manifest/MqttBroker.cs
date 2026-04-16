using mqtt2otel.Helper;
using System;
using System.Collections.Generic;
using System.Text;

namespace mqtt2otel.Manifest
{
    /// <summary>
    /// Provides the mqtt broker.
    /// </summary>
    public class MqttBroker : NamedIdObject
    {
        /// <summary>
        /// Gets or sets the mqtt broker endpoint.
        /// </summary>
        public MqttBrokerEndpoint Endpoint { get; set; } = new ();

        /// <summary>
        /// Gets or sets the delay in ms, that will be waited, if a reconnect was not successful, before the next connect is attempted.
        /// </summary>
        public int ReconnectDelayInMs { get; set; } = 5000;

        /// <summary>
        /// Gets or sets a prefix that will be prepended to the client guid used to connect to the broker. This helps in tracking down issues,
        /// when mutliple connections to clients are active, maybe even from different mqtt2otel servers.
        /// </summary>
        public string ClientPrefix { get; set; } = ApplicationStringConstants.ApplicationName;

        /// <summary>
        /// Validates the object.
        /// </summary>
        /// <param name="result">The validation result.</param>
        public void Validate(ValidationResult result)
        {
            this.Endpoint.Validate("Mqtt broker", result);

            if (this.Endpoint.Protocol.Trim().ToLower() != "tcp") result.AddError($"Unsupported protocol type ({this.Endpoint.Protocol}) for Mqtt broker endpoint. Supported protocols are: [tcp].");
        }
    }
}
