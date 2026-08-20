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
    }
}
