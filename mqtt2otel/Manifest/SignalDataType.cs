using System;
using System.Collections.Generic;
using System.Text;

namespace mqtt2otel.Manifest
{
    /// <summary>
    /// Provides all supported data types for otel signals.
    /// </summary>
    public enum SignalDataType
    {
        Float = 0,

        Int = 1,

        Double = 2,

        Long = 3,

        Decimal = 4,

        String = 5,

        DateTime = 6
    }
}
