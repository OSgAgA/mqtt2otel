using OpenTelemetry;
using OpenTelemetry.Logs;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace mqtt2otel.Helper
{
    /// <summary>
    /// This log processor is able to override the timestamp of a <see cref="Microsoft.Extensions.Logging.ILogger"/>.
    /// </summary>
    public class TimestampOverrideProcessor : BaseProcessor<LogRecord>
    {
        /// <summary>
        /// Gets the current scope used by the logger. Will be set via the <see cref="GetScope(LogRecordScope, object?)"/> callback.
        /// </summary>
        private Dictionary<string, object> scope = new();

        /// <summary>
        /// Called before the log message is written. Will override the timestamp, when a supported timestamp key is found
        /// inside the logging scope. 
        /// </summary>
        /// <param name="record">The log record.</param>
        public override void OnEnd(LogRecord record)
        {
            record.ForEachScope<object?>((scope, state) => this.GetScope(scope, state), null);

            string timestampKey = "otel_timestamp"; // Should we make this configurable?

            DateTime? timestamp = null;

            if (this.scope.ContainsKey(timestampKey))
            {
                var timestampObject = this.scope[timestampKey];

                if (timestampObject is DateTime timestampAsDateTime) timestamp = timestampAsDateTime;
                if (timestampObject is string timestampAsString)
                {
                    DateTime result;
                    DateTime.TryParse(timestampAsString,
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                        out result);

                    timestamp = result;
                }
            }

            if (timestamp != null) record.Timestamp = timestamp.Value;

            base.OnEnd(record);
        }

        /// <summary>
        /// Called from ForEachScope to gather scope inforamation for the current loger.
        /// </summary>
        /// <param name="scope">The scope that should be added.</param>
        /// <param name="state">The current state. Will be ignored.</param>
        private void GetScope(LogRecordScope scope, object? state)
        {
            if (scope.Scope is List<KeyValuePair<string, object>> typedScope && typedScope != null)
            { 
                this.scope = typedScope.ToDictionary<string, object>();
            }
        }
    }
}
