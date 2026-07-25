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
        /// Gets the payload available to the parser.
        /// </summary>
        public string Payload { get; private set; }

        /// <summary>
        /// Gets the topic available to the parser.
        /// </summary>
        public string Topic { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ParsingContext"/> class.
        /// </summary>
        /// <param name="variables">The variables available to the parser.</param>
        /// <param name="payload">The current payload, that will be parsed.</param>
        /// <param name="topic">The topic that triggered the parsing.</param>
        public ParsingContext(IEnumerable<Variable> variables, string payload, string topic)
        {
            this.Variables = variables;
            this.Payload = payload; ;
            this.Topic = topic;
        }
    }
}
