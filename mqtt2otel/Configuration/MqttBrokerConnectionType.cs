using System;
using System.Collections.Generic;
using System.Text;

namespace mqtt2otel.Configuration
{
    /// <summary>
    /// Represents the available connection types when connicting to a mqtt broker.
    /// </summary>
    public enum MqttBrokerConnectionType
    {
        /// <summary>
        /// A connection using TCP protocoll.
        /// </summary>
        Tcp,

        /// <summary>
        /// A connection using web sockets.
        /// </summary>
        WebSockets
    }
}
