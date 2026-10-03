using System;
using System.Collections.Generic;
using System.Text;

namespace mqtt2otel.Stores
{
    /// <summary>
    /// Identifies a single signal within the <see cref="SignalStore"/>, combining the subscription,
    /// the rule, and the signal name. Using a record struct gives value-based (structural) equality
    /// and hashing for free — no string formatting needed per lookup.
    /// </summary>
    /// <param name="OtelConnection">The otel connection identifier.</param>
    /// <param name="Name">The signal name.</param>
    public readonly record struct SignalKey(string OtelConnection, string Name);
}
