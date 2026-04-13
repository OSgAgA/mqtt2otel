using System;
using System.Collections.Generic;
using System.Text;

namespace mqtt2otel
{
    /// <summary>
    /// Container for storing a strategy and a value to which the strategy should be applied.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class StrategyValueContainer<T>
    {
        /// <summary>
        /// Gets or sets the strategy.
        /// </summary>
        public T Strategy { get; set; }

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="StrategyValueContainer{T}"/> class.
        /// </summary>
        /// <param name="strategy">The strategy.</param>
        /// <param name="value">The value.</param>
        public StrategyValueContainer(T strategy, string value) 
        {
            this.Strategy = strategy;
            this.Value = value;
        }
    }
}
