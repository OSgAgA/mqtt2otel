using mqtt2otel.Manifest;
using System;
using System.Collections.Generic;
using System.Text;

namespace mqtt2otel
{
    /// <summary>
    /// Represents the event arguments that will be provided, when a mqtt message has been received
    /// by the <see cref="MqttCoordinator"/>.
    /// </summary>
    /// <param name="message">The received message.</param>
    /// <param name="subscription">The subscription that triggered the message.</param>
    /// <param name="processor">The processor that is processing the message.</param>
    public class MqttMessageReceivedEventArgs(MqttMessage message, MqttSubscription? subscription, Processor? processor) 
    {
        /// <summary>
        /// Gets the payload of the message.
        /// </summary>
        public MqttMessage Message { get; private set; } = message;

        /// <summary>
        /// Gets the subscription that triggered the message.
        /// </summary>
        public MqttSubscription? Subscription{ get; private set; } = subscription;

        /// <summary>
        /// Gets the processor of the message.
        /// </summary>
        public Processor? Processor { get; private set; } = processor;
    }
}
