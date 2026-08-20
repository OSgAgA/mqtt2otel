using Microsoft.Extensions.Logging;
using mqtt2otel.Helper;
using mqtt2otel.InternalLogging;
using mqtt2otel.InternalMetrics;
using MQTTnet.Internal;
using OpenTelemetry.Metrics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

            if (result == null) result = new Manifest();

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
        /// Gets or sets a value indicating, whether attributes should be created from mqtt user properties (true), or not (false), or
        /// if the default setting should be used (null).
        /// </summary>
        [InheritedProperty]
        public bool? CreateAttributesFromUserProperties { get; set; } = true;

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
        [InheritedProperty("OtelConnection")]
        public string? DefaultOtelConnection
        {
            get => this.OtelConnections.FirstOrDefault()?.Name;
        }

        /// <summary>
        /// Initializes the manifest. This will apply all inherited informations, like variables and group subscriptions to child elements.
        /// </summary>
        public void Initialize()
        {
            if (Manifest.ObjectFactory == null)
                return;

            ImportEnabledList<NamedIdObject>.InitializeImports(this, this.internalLogger, Manifest.ObjectFactory);

            Manifest.SetObjectHierarchy(this);

            foreach (var subscriptionGroup in this.SubscriptionGroups)
            {
                this.ApplyVariablesToSubscriptions(subscriptionGroup.Subscriptions, subscriptionGroup.Variables);
            }

            foreach (var processor in this.Processors)
            {
                this.ApplySubscriptionGroupsToSubscriptions(processor.Mqtt.SubscriptionGroups, processor.Mqtt.Subscriptions);
                this.ApplyVariablesToSubscriptions(processor.Mqtt.Subscriptions, processor.Mqtt.Variables);
            }
        }

        /// <summary>
        /// Applies the variables of a parent subscription (if any) to the subscriptions of the group. 
        /// </summary>
        /// <param name="subscriptions">The subscription group.</param>
        /// <param name="variables">The variables that should be applied to all subscriptions.</param>
        private void ApplyVariablesToSubscriptions(List<MqttSubscription> subscriptions, List<Variable> variables)
        {
            foreach (var subscription in subscriptions)
            {
                subscription.Variables = variables.Combine(subscription.Variables).ToList();
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
        /// Sets the object parent child hierarchy of all <see cref="NamedIdObject"/> properties and sets all inherited attributes.
        /// </summary>
        /// <param name="current">The current object, on which the hierarchy should be set.</param>
        private static void SetObjectHierarchy(object current)
        {
            var type = current.GetType();

            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {

                // Case 1: Property type derives from NamedIdObject
                if (typeof(NamedIdObject).IsAssignableFrom(prop.PropertyType))
                {
                    var child = prop.GetValue(current) as NamedIdObject;
                    UpdateProperty(current, child);
                }

                UpdateProperty(current, prop, typeof(ImportEnabledList<>));
                UpdateProperty(current, prop, typeof(List<>));                
            }
        }

        /// <summary>
        /// Updates a list of NamedIdObject instances, by setting the parent to the provided object and setting the inherited
        /// properties (<see cref="UpdateInheritedProperties(object?, object)"/>.
        /// </summary>
        /// <param name="parent">The parent object.</param>
        /// <param name="prop">The property of the parent object, that contains the child objects. If null, nothing is executed. Must be of
        /// the provided propType type. If not, then execution is skipped."/></param>
        /// <param name="listType">The type of the list that should be updated.</param>
        private static void UpdateProperty(object parent, PropertyInfo prop, Type listType)
        {
            var propType = prop.PropertyType;

            if (propType.IsGenericType &&
                                propType.GetGenericTypeDefinition() == listType)
            {
                var genericArg = propType.GetGenericArguments()[0];

                if (!typeof(NamedIdObject).IsAssignableFrom(genericArg)) return;

                var typedList = prop.GetValue(parent) as IEnumerable<object>;

                if (typedList == null) return;

                foreach (var item in typedList)
                {
                    var child = item as NamedIdObject;
                    UpdateProperty(parent, child);
                }
            }

            return;
        }

        /// <summary>
        /// Updates a NamedIdObject instance, by setting the parent to the provided object and setting the inherited
        /// properties (<see cref="UpdateInheritedProperties(object?, object)"/>.
        /// </summary>
        /// <param name="parent">The parent object.</param>
        /// <param name="child">The child object. If null, nothing is executed.</param>
        private static void UpdateProperty(object parent, NamedIdObject? child)
        {
            
            if (child == null) return;
            child.Parent = parent;
            Manifest.UpdateInheritedProperties(parent, child);
            Manifest.SetObjectHierarchy(child);
            return;
        }

        /// <summary>
        /// Updates inherited properties, based on their value.
        /// 
        /// If an inherited child property of a <see cref="NamedIdObject"/> is found and the value of the property is null, then
        /// the value of the parent is written to the child (if an inherted property with the same name and type is found on the parent).
        /// </summary>
        /// <param name="parent">The parent object, that may inherit its value to the child.</param>
        /// <param name="child">The child object, that may inherit a value from the parent.</param>
        private static void UpdateInheritedProperties(object? parent, object child)
        {
            if (child == null || parent == null) return;

            var parentProperties = GetInheritedProperties(parent);
            var childProperties = GetInheritedProperties(child);

            foreach (var parentProperty in parentProperties)
            {
                string parentPropertyName = parentProperty.GetCustomAttribute<InheritedPropertyAttribute>()?.Name ?? parentProperty.Name;

                foreach(var childProperty in childProperties
                    .Where( childProp => (childProp.GetCustomAttribute<InheritedPropertyAttribute>()?.Name ?? childProp.Name) == parentPropertyName && childProp.PropertyType == parentProperty.PropertyType))
                {
                   var childValue = childProperty.GetValue(child);

                    if (childValue == null) childProperty.SetValue(child, parentProperty.GetValue(parent));
                }
            }
        }

        /// <summary>
        /// Get all properties of a given object, that have the <see cref="InheritedPropertyAttribute"/> set.
        /// </summary>
        /// <param name="current">The object that should be searched.</param>
        /// <returns>An enumerable containing all propeties with the given attribute.</returns>
        private static IEnumerable<PropertyInfo> GetInheritedProperties(object current)
        {
            var type = current.GetType();

            return type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                             .Where(p => p.GetCustomAttribute<InheritedPropertyAttribute>() != null);
        }
    }
}
