using mqtt2otel.Interfaces;
using NCalc;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace mqtt2otel.Parser
{
    /// <summary>
    /// Represents a class that can parse a string payload to an expected type.
    /// </summary>
    public class PayloadParser : StrategyParser<IParsingStrategy>, IPayloadParser
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PayloadParser"/> class.
        /// </summary>
        /// <param name="autodetectStrategies">A value indicating, whether all types that implement <see cref="IParsingStrategy"/> should be automatically added to the parser.</param>
        public PayloadParser(bool autodetectStrategies = true)
        {
            if (autodetectStrategies)
            {
                this.AutoDetectStrategies();
            }
        }

        /// <inheritdoc/>
        public async Task<T> Parse<T>(string name, string payload, string filterDefinition, ParsingContext context)
        {
            return await this.ParseExpression<T>(name, payload, filterDefinition, context);
        }

        /// <inheritdoc/>
        protected override TResult ApplyStrategy<TResult>(IParsingStrategy strategy, string payload, string pattern, ParsingContext context)
        {
            {
                return strategy.Parse<TResult>(payload, pattern, context);
            }
        }
    }
}
