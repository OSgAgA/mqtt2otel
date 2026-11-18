using System;
using System.Collections.Generic;
using System.Text;

namespace mqtt2otel.Provider
{
    /// <summary>
    /// Represents a state of a data provider.
    /// </summary>
    public class ProviderState
    {
        /// <summary>
        /// The separator for storing the state as a string.
        /// </summary>
        private const string separator = ";";

        /// <summary>
        /// Gets or sets a value indicating whether the state should be ignored and a state reset should be forced.
        /// </summary>
        public bool ForceReset { get; set; } = false;

        /// <summary>
        /// Gets or sets a string representation of the state.
        /// </summary>
        public string State { get; set; } = string.Empty;

        /// <summary>
        /// Generates a provider state.
        /// </summary>
        /// <param name="data">The data representing the state.</param>
        /// <returns>The generated state.</returns>
        public static ProviderState Generate(object[] data)
        {
            var result = new ProviderState();

            result.State = string.Join(separator, data);

            return result;
        }

        /// <summary>
        /// Gets a value from the state string representation.
        /// </summary>
        /// <typeparam name="T">The value type.</typeparam>
        /// <param name="index">The zero based index of the value inside the state.</param>
        /// <returns>The retrieved value.</returns>
        /// <exception cref="IndexOutOfRangeException">Thrown if index position is not existing in state.</exception>
        /// <exception cref="NotSupportedException">Thrown if data type is not supported as a return value.</exception>
        public T GetValue<T>(int index)
        {
            var split = this.State.Split(separator);

            if (split.Length <= index) throw new IndexOutOfRangeException();

            if (typeof(T) == typeof(int))
            {
                return (T)(object)int.Parse(split[index]);
            }

            throw new NotSupportedException($"Type {typeof(T)} is not supported.");
        }
    }
}
