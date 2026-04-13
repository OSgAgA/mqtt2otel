using Microsoft.Extensions.Logging;
using mqtt2otel.Helper;
using mqtt2otel.InternalLogging;
using MQTTnet.Internal;
using OpenTelemetry.Metrics;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using YamlDotNet.Serialization;

namespace mqtt2otel.Configuration
{
    /// <summary>
    /// Provides the main settings for mqtt2otel.
    /// </summary>
    public class Manifest
    {
        /// <summary>
        /// Reads the settings from a yaml file.
        /// </summary>
        /// <param name="internalLogger">The logger used for internal logging.</param>
        /// <param name="path">The path to the yaml file.</param>
        /// <returns></returns>
        public static Manifest ReadFromYaml(ILogger<Manifest> internalLogger, string path = "Manifest.yaml")
        {
            internalLogger.LogInformation($"Reading {Path.GetFullPath(path)}");

            var yaml = File.ReadAllText(path);
            var deserializer = new DeserializerBuilder().Build();

            var result = deserializer.Deserialize<Configuration.Manifest>(yaml);
            result.internalLogger = internalLogger;

            return result;
        }

        /// <summary>
        /// A reference to the logger used for internal log messages.
        /// </summary>
        private ILogger<Manifest> internalLogger = new EmptyLogger<Manifest>();

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

            this.MqttBroker.ForEach( broker => broker.Validate(result));
            this.OtelServer.ForEach( server => server.Validate(result));
            this.SubscriptionGroups.ForEach(group => group.Validate("Subscription groups", result));
            this.Metrics.ForEach(metric => metric.Validate(result));
            this.Logs.ForEach(log => log.Validate(result));

            foreach (var metricRuleSetting in this.Metrics)
            {
                if (!this.ServerNameExists(metricRuleSetting.OtelServerName))
                {
                    result.AddError($"Metric rule {metricRuleSetting.Name} refers to a non existing Otel server name: {metricRuleSetting.OtelServerName}");
                }

                foreach (var otelRuleSetting in metricRuleSetting.Otel.Rules)
                {
                    if (!this.ServerNameExists(otelRuleSetting.OtelServerName))
                    {
                        result.AddError($"Metric rule {otelRuleSetting.Name} refers to a non existing Otel server name: {otelRuleSetting.OtelServerName}");
                    }
                }
            }

            foreach (var logRuleSettings in this.Logs)
            {
                if (!this.ServerNameExists(logRuleSettings.Otel.OtelServerName))
                {
                    result.AddError($"Metric rule {logRuleSettings.Name} refers to a non existing Otel server name: {logRuleSettings.Otel.OtelServerName}");
                }

                foreach (var otelRuleSetting in logRuleSettings.Otel.Rules)
                {
                    if (!this.ServerNameExists(otelRuleSetting.OtelServerName))
                    {
                        result.AddError($"Metric rule {otelRuleSetting.Name} refers to a non existing Otel server name: {otelRuleSetting.OtelServerName}");
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Gets or sets the settings version.
        /// </summary>
        public string Version { get; set; } = "";

        /// <summary>
        /// Gets or sets all available subscription groups settings.
        /// </summary>
        public List<MqttSettings> SubscriptionGroups { get; set; } = new();

        /// <summary>
        /// Gets or sets the mqtt broker settings.
        /// </summary>
        public List<MqttBrokerSettings> MqttBroker { get; set; } = new();

        /// <summary>
        /// Gets or sets the open telemetry server settings.
        /// </summary>
        public List<OtelServerSettings> OtelServer { get; set; } = new();

        /// <summary>
        /// Gets or sets all metrics settings.
        /// </summary>
        public List<MetricsRuleSettings> Metrics { get; set; } = new();

        /// <summary>
        /// Gets or sets all logs settings.
        /// </summary>
        public List<LoggingRuleSettings> Logs { get; set; } = new();

        /// <summary>
        /// Gets the default otel server. That is the first server defined in <see cref="OtelServer"/> or null, if no otel server
        /// is defined.
        /// </summary>
        public OtelServerSettings? DefaultOtelServer
        {
            get => this.OtelServer.FirstOrDefault();
        }

        /// <summary>
        /// Initializes the settings objects. This will apply all inherited informations, like variables and group subscriptions to child elements.
        /// </summary>
        public void Initialize()
        {
            this.ApplyOtelServerNamesToRules();

            foreach (var metric in this.Metrics)
            {
                this.ApplySubscriptionGroupsToSubscriptions(metric.Mqtt.SubscriptionGroups, metric.Mqtt.Subscriptions);
                this.ApplyTransformationFromParent(metric.Mqtt.Transform, metric.Mqtt.Subscriptions);
            }

            foreach (var log in this.Logs)
            {
                this.ApplySubscriptionGroupsToSubscriptions(log.Mqtt.SubscriptionGroups, log.Mqtt.Subscriptions);
                this.ApplyTransformationFromParent(log.Mqtt.Transform, log.Mqtt.Subscriptions);
            }
        }

        /// <summary>
        /// Tests if the provided open telemetry server name exists.
        /// </summary>
        /// <param name="name">The server name.</param>
        /// <returns>A value indicating whether the name exists.</returns>
        private bool ServerNameExists(string? name)
        {
            if (name == null) return false;
            
            bool result = false;

            foreach (var otelServerSettings in this.OtelServer)
            {
                if (otelServerSettings.Name == name) 
                { 
                    result = true; 
                    break; 
                }
            }

            return result;
        }

        /// <summary>
        /// Applies the open telemetry server name down the hierarchy, explicitly setting the default otel server name for 
        /// null values.
        /// </summary>
        private void ApplyOtelServerNamesToRules()
        {
            if (this.DefaultOtelServer == null) return;

            foreach(var metric in this.Metrics)
            {
                if (metric.OtelServerName == null) metric.OtelServerName = this.DefaultOtelServer.Name;

                foreach (var metricRule in metric.Otel.Rules)
                {
                    if (metricRule.OtelServerName == null) metricRule.OtelServerName = metric.OtelServerName;
                }
            }

            foreach (var logging in this.Logs)
            {
                if (logging.Otel.OtelServerName == null) logging.Otel.OtelServerName = this.DefaultOtelServer.Name;

                foreach (var loggingRule in logging.Otel.Rules)
                {
                    if (loggingRule.OtelServerName == null) loggingRule.OtelServerName = logging.Otel.OtelServerName;
                }
            }
        }

        /// <summary>
        /// Applies all subscriptions inside the given subscription groups the the given list of subscriptions.
        /// </summary>
        /// <param name="subscriptionGroups">The subscription groups that should be applied to the subscriptions</param>
        /// <param name="subscriptions">The subscriptions where the subscription group information should be added.</param>
        private void ApplySubscriptionGroupsToSubscriptions(IEnumerable<SubscriptionGroupSettings> subscriptionGroups, List<MqttSubscriptionSettings> subscriptions)
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

                    var newSubscription = new MqttSubscriptionSettings()
                    {
                        Name = subscription.Name,
                        Description = subscription.Description,
                        Topic = newPath,
                        Transform = subscription.Transform,
                        Variables = subscriptionGroup.Variables.Combine(subscription.Variables).ToList(),
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
        private void ApplyTransformationFromParent(string parentTransform, List<MqttSubscriptionSettings> subscriptions)
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
