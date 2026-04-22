using NCalc;
using System;
using System.Collections.Generic;
using System.Text;

namespace mqtt2otel.Manifest
{
    /// <summary>
    /// Provides all mqtt settings and subscriptions.
    /// </summary>
    public class Mqtt : NamedIdObject
    {
        /// <summary>
        /// Gets or sets all variables.
        /// </summary>
        public List<Variable> Variables { get; set; } = new();

        /// <summary>
        /// Gets or sets a transformation expression (<see cref="Interfaces.IPayloadTransformation"/>). 
        /// 
        /// If not empty, this transformation will be applied to all mqtt messages, before it is further processed.
        /// </summary>
        public string Transform { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a list of mqtt broker subscriptions.
        /// </summary>
        public ImportEnabledList<MqttSubscription> Subscriptions { get; set; } = new();

        /// <summary>
        /// Gets or sets a list of broker subscription groups.
        /// </summary>
        public List<SubscriptionGroupReference> SubscriptionGroups { get; set; } = new();

        /// <summary>
        /// Gets or sets the broker to which this subscription is bound. A null value represents the default broker.
        /// </summary>
        public string? Broker { get; set; } = null;

        /// <summary>
        /// Validates the object.
        /// </summary>
        /// <param name="context">The currently active context. This will be provided as a hint to the user, where a problem occured.</param>
        /// <param name="result">The validation result.</param>
        public void Validate(string context, ValidationResult result)
        {
            context = $"{context}/({this.Name})";
            this.Variables.ForEach(var => var.Validate(context, result));
            this.Subscriptions.ForEach(sub => sub.Validate(context, result));

            if (!string.IsNullOrWhiteSpace(this.Transform))
            {
                var expression = new AsyncExpression(this.Transform);
                if (expression.HasErrors())
                {
                    if (expression.Error == null) return;

                    if (expression.Error.InnerException != null)
                    {
                        result.AddError($"{context}/({this.Name})/{nameof(Transform)}: Expression is \"{this.Transform}\". {expression.Error.InnerException.Message}");
                    }
                    else
                    {
                        result.AddError($"{context}/({this.Name})/{nameof(Transform)}: Expression is \"{this.Transform}\". {expression.Error}");
                    }
                }
            }
        }
    }
}
