using System;
using System.Collections.Generic;
using System.Text;

namespace mqtt2otel.Parser
{
    public class ParsingContext
    {
        public IEnumerable<Variable> Variables { get; private set; }

        public ParsingContext(IEnumerable<Variable> variables)
        {
            this.Variables = variables;
        }
    }
}
