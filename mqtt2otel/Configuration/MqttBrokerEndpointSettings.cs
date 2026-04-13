using MQTTnet.Formatter;
using System;
using System.Collections.Generic;
using System.Security.Authentication;
using System.Text;

namespace mqtt2otel.Configuration
{
    /// <summary>
    /// Provides the default values for the mqtt broker endpoint settings.
    /// </summary>
    public class MqttBrokerEndpointSettings : EndpointSettings
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MqttBrokerEndpointSettings"/> class.
        /// </summary>
        public MqttBrokerEndpointSettings() : base()
        {
            this.Port = 1813;
            this.Protocol = "tcp";
        }

        /// <summary>
        /// Gets or sets the connection type used to connect to the mqtt broker. <see cref="MqttBrokerConnectionType"/>
        /// for available values.
        /// </summary>
        public MqttBrokerConnectionType ConnectionType { get; set; } = MqttBrokerConnectionType.Tcp;

        /// <summary>
        /// Gets or sets an explicit protocoll version when connecting to the MQTT broker. Set to null
        /// to use default settings.
        /// </summary>
        public MqttProtocolVersion? MqttProtocollVersion { get; set; } = null;

        /// <summary>
        /// Gets or sets a value indicating whether transport level security should be enabled for the endpoint.
        /// </summary>
        public bool EnableTls { get; set; } = false;

        /// <summary>
        /// Gets or sets the ssl protocol version for TLS. See <see cref="SslProtocols"/> for available values.
        /// Set to null to let the OS determine the correct value. 
        /// 
        /// Will be ignored, if <see cref="EnableTls"/> is false.
        /// </summary>
        public SslProtocols? TlsSslProtocol { get; set; } = null;

        /// <summary>
        /// Gets or sets the path to a certificate authority file used in transport level security.
        /// Set to null to not use an explicit CA file.
        /// 
        /// Will be ignored, if <see cref="EnableTls"/> is false.
        /// </summary>
        public string? TlsCaFilePath { get; set; } = null;

        /// <summary>
        /// Gets or sets a value indicating whether packet fragmentation should be used for the endpoint.
        /// This should be disabled for connecting to AWS.
        /// </summary>
        public bool UsePacketFragmentation { get; set; } = true;

        /// <summary>
        /// Gets or sets the name of the user credentials. Set to null if you want to connect without credentials.
        /// </summary>
        public string? Username { get; set; } = null;

        /// <summary>
        /// Gets or sets the password.
        /// 
        /// Will be ignored if <see cref="Username"/> is null.
        /// </summary>
        public string Password { get; set; } = string.Empty;
    }
}
