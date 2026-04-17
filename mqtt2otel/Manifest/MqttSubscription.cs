using System;
using System.Collections.Generic;
using System.Text;

namespace mqtt2otel.Manifest
{
    /// <summary>
    /// Provides the mqtt broker subscriptions.
    /// </summary>
    public class MqttSubscription : NamedIdObject
    {
        /// <summary>
        /// Gets or sets the topic to which the application should subscribe.
        /// </summary>
        public string? Topic { get; set; } = null;

        /// <summary>
        /// Gets or sets the broker to which this subscription is bound. A null value represents the default broker.
        /// </summary>
        public string? Broker { get; set; } = null;

        /// <summary>
        /// Gets or sets a transform expression (<see cref="Interfaces.IPayloadTransformation"/>).
        /// 
        /// If non empty this transformation will be applied to all messages received via this subscription.
        /// </summary>
        public string? Transform { get; set; } = null;

        /// <summary>
        /// Gets or sets the variables defined in this subscription.
        /// </summary>
        public List<Variable> Variables { get; set; } = new();

        /// <summary>
        /// Validates the object.
        /// </summary>
        /// <param name="context">The currently active context. This will be provided as a hint to the user, where a problem occured.</param>
        /// <param name="result">The validation result.</param>
        public void Validate(string context, ValidationResult result)
        {
            context = $"{context} / Mqtt subscription ({this.Name})";
            if (string.IsNullOrWhiteSpace(this.Topic)) result.AddError($"{context}: Empty topic found. Plesae set the topic to a non empty value.");
            this.Variables.ForEach(var => var.Validate(context, result));
        }
    }
}
