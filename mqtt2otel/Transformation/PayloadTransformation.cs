using mqtt2otel.Helper;
using mqtt2otel.Interfaces;
using mqtt2otel.Parser;
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace mqtt2otel.Transformation
{
    /// <summary>
    /// Represents a class that can transform a string payload to another string payload based on the provided
    /// <see cref="ITransformationStrategy"/> strategies.
    /// </summary>
    public class PayloadTransformation : StrategyParser<ITransformationStrategy>, IPayloadTransformation
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PayloadTransformation"/> class.
        /// </summary>
        /// <param name="autoDetectStrategies">A value indicating, whether all types that implement <see cref="ITransformationStrategy"/> should be automatically added.</param>
        public PayloadTransformation(bool autoDetectStrategies = true)
        {
            if (autoDetectStrategies)
            {
                this.AutoDetectStrategies();
            }
        }

        /// <summary>
        /// Applies a transformation to a payload.
        /// </summary>
        /// <param name="category">The subscription type that triggered the transformation.</param>
        /// <param name="name">An identifier to help the user to idnetify the correct position of the transformation in case of an error.</param>
        /// <param name="expression">The expression that should be used for transforming the payload.</param>
        /// <param name="context">The execution context in which the strategy will be exeucted.</param>
        /// <returns>The transformed payload</returns>
        public string Apply(string name, string expression, ParsingContext context)
        {
            return this.ParseExpression<string>(name, expression, context);
        }

        /// <summary>
        /// Applies a registered strategy to a payload. Called by <see cref="StrategyParser{T}"/> base class.
        /// </summary>
        /// <typeparam name="TResult">The expected result type.</typeparam>
        /// <param name="strategy">The strategy to be applied.</param>
        /// <param name="pattern">The pattern that will be passed to the strategy for performing the transformation.</param>
        /// <param name="context">The execution context in which the strategy will be exeucted.</param>
        /// <returns>The transformed payload.</returns>
        protected override TResult ApplyStrategy<TResult>(ITransformationStrategy strategy, string pattern, ParsingContext context)
        {
            return TypeHelper.ConvertObject<TResult>(strategy.Apply(pattern, context));
        }
    }
}
