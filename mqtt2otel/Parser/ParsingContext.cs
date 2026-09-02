using System;
using System.Collections.Generic;
using System.Text;

namespace mqtt2otel.Parser
{
    /// <summary>
    /// Represents the context information for the <see cref="Parser.PayloadParser"/> class.
    /// </summary>
    public class ParsingContext
    {
        /// <summary>
        /// Gets the variables avaialable to the parser.
        /// </summary>
        public IEnumerable<Variable> Variables { get; private set; }

        /// <summary>
        /// Gets the message information available to the parser.
        /// </summary>
        public MqttMessage Message { get; private set; }

        /// <summary>
        /// Gets the internaly created variables.
        /// </summary>
        public Dictionary<string, object?> InternalVariables { get; private set; } = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="ParsingContext"/> class.
        /// </summary>
        /// <param name="variables">The variables available to the parser.</param>
        /// <param name="message">The current mqtt message, that will be parsed.</param>
        /// <param name="topic">The topic that triggered the parsing.</param>
        public ParsingContext(IEnumerable<Variable> variables, MqttMessage message)
        {
            this.Variables = variables;
            this.Message = message;
        }

        /// <summary>
        /// Clones the object by creating a copy of the variables and internal string variables. The message still refers to the original message
        /// object.
        /// </summary>
        /// <returns>The cloned object.</returns>
        public ParsingContext Clone()
        {
            var variables = this.Variables.Select(var => new Variable() { Key = var.Key, Value = var.Value });

            var result = new ParsingContext(variables, this.Message);

            foreach (var item in this.InternalVariables)
            {
                result.InternalVariables.Add(item.Key, item.Value);
            }

            return result;
        }
    }
}
