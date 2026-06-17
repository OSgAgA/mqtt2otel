using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace mqtt2otel.Metadata
{
    public class LogTestData
    {
        public DateTime Timestamp { get; set; } = DateTime.Now;

        public LogLevel LogLevel { get; set; } = LogLevel.Information;

        public string? Body { get; set; } = null;

        public string? CategoryName { get; set; } = null;

        public Dictionary<string, object?> Attributes { get; set; } = new();

        public string SpanId { get; set; } = string.Empty;

        public string TraceId { get; set; } = string.Empty;

        public string? TraceState { get; set; } = null;

        public ActivityTraceFlags TraceFlags { get; set; } = new();

        public LogTestData() { }

        public LogTestData(LogRecord entry)
        {
            this.Timestamp = entry.Timestamp;
            this.LogLevel = entry.LogLevel;
            this.Body = entry.Body;
            this.CategoryName = entry.CategoryName;

            if (entry.Attributes != null)
            {
                entry.ForEachScope<object?>((scope, state) => this.GetScope(scope, state), null);
            }

            this.SpanId = entry.SpanId.ToString();
            this.TraceId = entry.TraceId.ToString();
            this.TraceState = entry.TraceState;
            this.TraceFlags = entry.TraceFlags;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.Append("Timestamp: ");
            sb.AppendLine(this.Timestamp.ToString());

            sb.Append("  Level: ");
            sb.AppendLine(this.LogLevel.ToString());

            sb.Append("  Body: ");
            sb.AppendLine(this.Body);

            sb.Append("  Category name: ");
            sb.AppendLine(this.CategoryName);

            if (this.Attributes.Count > 0)
            {
                sb.AppendLine("  Attributes: ");
                foreach (var attribute in this.Attributes)
                {
                    sb.AppendLine($"    {attribute.Key}: {attribute.Value}");
                }
            }

            sb.Append("  Span id: ");
            sb.AppendLine(this.SpanId.ToString());

            sb.Append("  Trace id: ");
            sb.AppendLine(this.TraceId.ToString());

            sb.Append("  Trace state: ");
            sb.AppendLine(this.TraceState);

            sb.Append("  Trace state: ");
            sb.AppendLine(this.TraceFlags.ToString());

            return sb.ToString();
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
                foreach (var attribute in typedScope.ToDictionary<string, object>())
                {
                    this.Attributes.Add(attribute.Key, attribute.Value);
                }
            }
        }
    }
}
