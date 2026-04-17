using mqtt2otel.Manifest;

namespace mqtt2otel.Interfaces
{
    /// <summary>
    /// Represents a processor. A processor is responsible for subscribing to mqtt topics and
    /// applying otel rules to these subscriptions.
    /// </summary>
    public interface IProcessor
    {
        /// <summary>
        /// Gets or sets the mqtt settings for the rule.
        /// </summary>
        Mqtt Mqtt { get; set; }

        /// <summary>
        /// Gets or sets the otel settings for the rule.
        /// </summary>
        Otel Otel { get; set; }

        /// <summary>
        /// Gets or sets the name of the open telemetriy server to be used for all rules in this section. 
        /// Set to null for using the default server.
        /// </summary>
        string? OtelServerName { get; set; }

        /// <summary>
        /// Process a subscription payload that was received from the mqtt broker.
        /// </summary>
        /// <param name="payload">The received payload.</param>
        /// <param name="subscription">The subscription that received the payload.</param>
        /// <returns>A value indicating whether the operation has been successful.</returns>
        Task<bool> ProcessSubscriptionPayload(string payload, MqttSubscription subscription);

        /// <summary>
        /// Validates the object.
        /// </summary>
        /// <param name="result">The validation result.</param>
        void Validate(ValidationResult result);
    }
}