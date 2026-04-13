using mqtt2otel.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace mqtt2otel
{
    /// <summary>
    /// Represents a context for storing mqtt subscriptions including their configuration.
    /// </summary>
    /// <typeparam name="TSubscriptionConfigurationRule">The subscription configuration type.</typeparam>
    public class MqttSubscriptionContext<TSubscriptionConfigurationRule>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MqttSubscriptionContext{TSettings}"/> class.
        /// </summary>
        /// <param name="settings">The subscription configuration rule.</param>
        /// <param name="mqttSettings">The mqtt subscription settings.</param>
        public MqttSubscriptionContext(TSubscriptionConfigurationRule settings, MqttSubscriptionSettings mqttSettings)
        {
            Settings = settings;
            MqttSubscriptionSettings = mqttSettings;
        }

        /// <summary>
        /// Gets or sets the subscription configuration rule.
        /// </summary>
        public TSubscriptionConfigurationRule Settings { get; set; }

        /// <summary>
        /// Gets or sets the subscription settings.
        /// </summary>
        public MqttSubscriptionSettings MqttSubscriptionSettings { get; set; }
    }
}
