using System;
using System.Collections.Generic;
using System.Text;

namespace mqtt2otel.Metadata
{
    /// <summary>
    /// Represents a position inside a file.
    /// </summary>
    /// <param name="line">The line number starting with one.</param>
    /// <param name="column">The column starting with one.</param>
    public class Position(long line, long column)
    {
        /// <summary>
        /// Gets or sets the line position inside a file. Starting with one.
        /// </summary>
        public long Line { get; set; } = line;

        /// <summary>
        /// Gets or sets a column position inside a file. Starting with one.
        /// </summary>
        public long Column { get; set; } = column;

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"line:{this.Line}, Column: {this.Column}";
        }
    }
}