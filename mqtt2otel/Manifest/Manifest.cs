using Microsoft.Extensions.Logging;
using mqtt2otel.Helper;
using mqtt2otel.InternalLogging;
using MQTTnet.Internal;
using OpenTelemetry.Metrics;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using YamlDotNet.Serialization;

namespace mqtt2otel.Manifest
{
    /// <summary>
    /// Provides the user provided rules for subscribing to a mqtt endpoint and for processing the received payloads..
    /// </summary>
    public class Manifest
    {
        /// <summary>
        /// Gets or sets the object factory that should be used for parsing the yaml file.
        /// </summary>
        public static IObjectFactory? ObjectFactory;

        /// <summary>
        /// Reads the manifest from a yaml file.
        /// 
        /// Object factory must be set before this method can be called.
        /// </summary>
        /// <param name="internalLogger">The logger used for internal logging..</param>
        /// <param name="path">The path to the yaml file.</param>
        /// <param name="yaml">This parameter can be used to provide the yaml to be parsed directly. If this is not null, then the path parameter will be ignored.</param>
        /// <returns>The created manifest.</returns>
        public static Manifest ReadFromYaml(ILogger internalLogger, string path = "Manifest.yaml", string? yaml = null)
        {
            if (Manifest.ObjectFactory == null)
            {
                internalLogger.LogCritical($"Internal error: Calling {nameof(ReadFromYaml)} without initializíng {nameof(ObjectFactory)} first. Providing default manifest.");
                return new Manifest();
            }

            if (yaml == null)
            {
                internalLogger.LogInformation($"Reading {Path.GetFullPath(path)}");

                yaml = File.ReadAllText(path);
            }
            else
            {
                internalLogger.LogInformation("Reading manifest from provided yaml.");
            }

            var deserializer = new DeserializerBuilder().WithObjectFactory(Manifest.ObjectFactory).Build();

            var result = deserializer.Deserialize<Manifest>(yaml);
            result.internalLogger = internalLogger;

            return result;
        }

        /// <summary>
        /// A reference to the logger used for internal log messages.
        /// </summary>
        private ILogger internalLogger = new EmptyLogger<Manifest>();

        /// <summary>
        /// Validates all settings.
        /// </summary>
        /// <returns>All validation results.</returns>
        public ValidationResult Validate()
        {
            var result = new ValidationResult();

            string supportedVersions = "Supported versions are: [1.0].";
            if (string.IsNullOrEmpty(this.Version)) return result.AddError($"No or empty Version property in file. Version must allways be set! {supportedVersions}");
            if (this.Version != "1.0") return result.AddError($"Provided version {this.Version} is not supported. {supportedVersions}");

            this.MqttConnections.ForEach(broker => broker.Validate(result));
            this.OtelConnections.ForEach(connection => connection.Validate(result));
            this.SubscriptionGroups.ForEach(group => group.Validate("Subscription groups", result));
            this.Processors.ForEach(metric => metric.Validate(result));

            foreach (var processor in this.Processors)
            {
                if (!this.OtelConnectionExists(processor.OtelConnection))
                {
                    result.AddError($"Processors {processor.Name} refers to a non existing Otel connection: {processor.OtelConnection}");
                }

                foreach (var otelRuleSetting in processor.Otel.Metrics)
                {
                    if (!this.OtelConnectionExists(otelRuleSetting.OtelConnection))
                    {
                        result.AddError($"Processors {otelRuleSetting.Name} refers to a non existing Otel connection: {otelRuleSetting.OtelConnection}");
                    }
                }

                foreach (var otelRuleSetting in processor.Otel.Logs)
                {
                    if (!this.OtelConnectionExists(otelRuleSetting.OtelConnection))
                    {
                        result.AddError($"Processors {otelRuleSetting.Name} refers to a non existing Otel connection: {otelRuleSetting.OtelConnection}");
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Gets or sets the manifest version.
        /// </summary>
        public string Version { get; set; } = "";

        /// <summary>
        /// Gets or sets all available subscription groups.
        /// </summary>
        public ImportEnabledList<SubscriptionGroup> SubscriptionGroups { get; set; } = new();

        /// <summary>
        /// Gets or sets the mqtt broker.
        /// </summary>
        public ImportEnabledList<MqttBroker> MqttConnections { get; set; } = new();

        /// <summary>
        /// Gets or sets the open telemetry server connections.
        /// </summary>
        public ImportEnabledList<OtelServerConnection> OtelConnections { get; set; } = new();

        /// <summary>
        /// Gets or sets all metrics.
        /// </summary>
        public ImportEnabledList<Processor> Processors { get; set; } = new();

        /// <summary>
        /// Gets the default otel server connection. That is the first server defined in <see cref="OtelConnections"/> or null, if no otel server
        /// is defined.
        /// </summary>
        public OtelServerConnection? DefaultOtelConnection
        {
            get => this.OtelConnections.FirstOrDefault();
        }

        /// <summary>
        /// Initializes the manifest. This will apply all inherited informations, like variables and group subscriptions to child elements.
        /// </summary>
        public void Initialize()
        {
            if (Manifest.ObjectFactory == null)
                return;

            ImportEnabledList<NamedIdObject>.InitializeImports(this, this.internalLogger, Manifest.ObjectFactory);
            this.ApplyOtelConnectionNamesToRules();

            foreach (var subscriptionGroup in this.SubscriptionGroups)
            {
                this.ApplyBrokerToSubscriptions(subscriptionGroup);
                this.ApplyTransformationToSubscriptions(subscriptionGroup);
                this.ApplyVariablesToSubscriptions(subscriptionGroup);
            }

            foreach (var processor in this.Processors)
            {
                this.ApplyBrokerToSubscriptions(processor.Mqtt);
                this.ApplySubscriptionGroupsToSubscriptions(processor.Mqtt.SubscriptionGroups, processor.Mqtt.Subscriptions);
                this.ApplyTransformationFromParent(processor.Mqtt.Transform, processor.Mqtt.Subscriptions);
            }
        }

        /// <summary>
        /// Applies the broker of the mqtt section (if set) to the subscriptions of the section. It will only be applied if the
        /// broker is not explicitly set inside a subscription.
        /// </summary>
        /// <param name="subscriptionGroup">The mqtt section of a processor.</param>
        private void ApplyBrokerToSubscriptions(Mqtt mqtt)
        {
            if (mqtt.BrokerConnection == null) return;

            foreach (var subscription in mqtt.Subscriptions)
            {
                if (subscription.BrokerConnection == null) subscription.BrokerConnection = mqtt.BrokerConnection;
            }
        }

        /// <summary>
        /// Applies the variables of the subscription group (if any) to the subscriptions of the group. 
        /// </summary>
        /// <param name="subscriptionGroup">The subscription group.</param>
        private void ApplyVariablesToSubscriptions(SubscriptionGroup subscriptionGroup)
        {
            foreach (var subscription in subscriptionGroup.Subscriptions)
            {
                subscription.Variables = subscriptionGroup.Variables.Combine(subscription.Variables).ToList();
            }
        }

        /// <summary>
        /// Applies the transform pattern of the subscription group (if set) to the subscriptions of the group. It will only be applied if the
        /// transform pattern is not explicitly set inside a subscription.
        /// </summary>
        /// <param name="subscriptionGroup">The subscription group.</param>
        private void ApplyTransformationToSubscriptions(SubscriptionGroup subscriptionGroup)
        {
            if (string.IsNullOrWhiteSpace(subscriptionGroup.Transform)) return;

            foreach (var subscription in subscriptionGroup.Subscriptions)
            {
                if (string.IsNullOrWhiteSpace(subscription.Transform)) subscription.Transform = subscriptionGroup.Transform;
            }
        }

        /// <summary>
        /// Applies the broker of the subscription group (if set) to the subscriptions of the group. It will only be applied if the
        /// broker is not explicitly set inside a subscription.
        /// </summary>
        /// <param name="subscriptionGroup">The subscription group.</param>
        private void ApplyBrokerToSubscriptions(SubscriptionGroup subscriptionGroup)
        {
            if (subscriptionGroup.Broker == null) return;

            foreach (var subscription in subscriptionGroup.Subscriptions)
            {
                if (subscription.BrokerConnection == null) subscription.BrokerConnection = subscriptionGroup.Broker;
            }
        }

        /// <summary>
        /// Tests if the provided open telemetry server connection name exists.
        /// </summary>
        /// <param name="name">The server connection name.</param>
        /// <returns>A value indicating whether the name exists.</returns>
        private bool OtelConnectionExists(string? name)
        {
            if (name == null) return false;

            bool result = false;

            foreach (var connection in this.OtelConnections)
            {
                if (connection.Name == name)
                {
                    result = true;
                    break;
                }
            }

            return result;
        }

        /// <summary>
        /// Applies the open telemetry server connection name down the hierarchy, explicitly setting the default otel server name for 
        /// null values.
        /// </summary>
        private void ApplyOtelConnectionNamesToRules()
        {
            if (this.DefaultOtelConnection == null) return;

            foreach (var processor in this.Processors)
            {
                if (processor.OtelConnection == null) processor.OtelConnection = this.DefaultOtelConnection.Name;

                foreach (var metricRule in processor.Otel.Metrics)
                {
                    if (metricRule.OtelConnection == null) metricRule.OtelConnection = processor.OtelConnection;
                }

                foreach (var metricRule in processor.Otel.Logs)
                {
                    if (metricRule.OtelConnection == null) metricRule.OtelConnection = processor.OtelConnection;
                }

            }
        }

        /// <summary>
        /// Applies all subscriptions inside the given subscription groups the the given list of subscriptions.
        /// </summary>
        /// <param name="subscriptionGroups">The subscription groups that should be applied to the subscriptions</param>
        /// <param name="subscriptions">The subscriptions where the subscription group information should be added.</param>
        private void ApplySubscriptionGroupsToSubscriptions(IEnumerable<SubscriptionGroupReference> subscriptionGroups, List<MqttSubscription> subscriptions)
        {
            foreach (var group in subscriptionGroups)
            {
                var query = this.SubscriptionGroups.Where(sub => sub.Name == group.Name);

                if (!query.Any())
                {
                    this.internalLogger.LogError($"Could not find subscription group with name {group.Name}. Skipping it.");
                    continue;
                }
                var subscriptionGroup = query.First();
                foreach (var subscription in subscriptionGroup.Subscriptions)
                {
                    if (subscription.Topic == null) continue;

                    string newPath = subscription.Topic;
                    if (!string.IsNullOrWhiteSpace(group.ParentPath)) newPath = group.ParentPath + "/" + newPath;
                    if (!string.IsNullOrWhiteSpace(group.SubPath)) newPath += "/" + group.SubPath;

                    var newSubscription = new MqttSubscription()
                    {
                        Name = subscription.Name,
                        Description = subscription.Description,
                        Topic = newPath,
                        Transform = subscription.Transform,
                        Variables = subscription.Variables,
                    };

                    subscriptions.Add(newSubscription);
                }
            }
        }

        /// <summary>
        /// Applies transformations from a parent to the given list of subscriptions.
        /// </summary>
        /// <param name="parentTransform">The transformation that should be applied to all subscriptions.</param>
        /// <param name="subscriptions">The list of subscriptions to which the transformation should be applied.</param>
        private void ApplyTransformationFromParent(string parentTransform, List<MqttSubscription> subscriptions)
        {
            if (string.IsNullOrEmpty(parentTransform)) return;

            foreach (var subscription in subscriptions)
            {
                if (!string.IsNullOrWhiteSpace(subscription.Transform)) continue;

                subscription.Transform = parentTransform;
            }
        }
    }
}
