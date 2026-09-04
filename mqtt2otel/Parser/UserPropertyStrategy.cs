using System;
using System.Collections.Generic;
using System.Text;

namespace mqtt2otel.Parser
{
    /// <summary>
    /// Represents a <see cref="IParsingStrategy"/> that is able to read mqtt user properties.
    /// 
    /// User properties are identified via their name. If multiple properties have the same name, the first match found is returned.
    public class UserPropertyStrategy : IParsingStrategy
    {
        /// <summary>
        /// The function name used by the strategy.
        /// </summary>
        public string Key => "USERPROPERTY";

        /// <summary>
        /// Returns the value of the first mqtt user property of the message that has the given name or an empty string if no user
        /// property with the given name is found.
        /// </summary>
        /// <param name="name">The mqtt user property name.</param>
        /// <param name="context">The parsing context.</param>
        /// <returns>The value of the first property with the given name or an empty string.</returns>
        /// <exception cref="ArgumentException">Thrown if T is not string.</exception>
        public object? Parse(string name, ParsingContext context)
        {
            var query = context.Message.UserProperties.Where(prop => prop.Name == name);

            if (query.Any())
            {
                return query.First().Value;
            }

            return string.Empty;
        }
    }
}
