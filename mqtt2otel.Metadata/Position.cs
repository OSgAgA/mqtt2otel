using System;
using System.Collections.Generic;
using System.Text;

namespace mqtt2otel.Metadata
{
    public class Position(long line, long column)
    {
        public long Line { get; set; } = line;

        public long Column { get; set; } = column;

        public override string ToString()
        {
            return $"line:{this.Line}, Column: {this.Column}";
        }
    }
}