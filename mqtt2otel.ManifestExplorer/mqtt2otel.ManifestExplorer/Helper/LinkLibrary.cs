namespace mqtt2otel.ManifestExplorer.Helper
{
    /// <summary>
    /// Represents a library containing default links, that can be used inside examples.
    /// </summary>
    public static class LinkLibrary
    {
        /// <summary>
        /// Gets all available links from the library.
        /// </summary>
        /// <returns>All available links.</returns>
        public static Dictionary<string, string> GetAll()
        {
            return new Dictionary<string, string>()
            {
                { "Mqtt broker connection", "https://mqtt2otel.org/docs/manifest/mqttbroker/" },
                { "Open telemetry connection", "https://mqtt2otel.org/docs/manifest/otelserver/" },
                { "Application settings", "https://mqtt2otel.org/docs/applicationsettings/" },
                { "Variables", "https://mqtt2otel.org/docs/manifest/variables/" },
                { "Subscriptions", "https://mqtt2otel.org/docs/manifest/subscription/" },
                { "Subscription groups", "https://mqtt2otel.org/docs/manifest/subscription/#subscription-groups" },
                { "Open telemetry metrics", "https://mqtt2otel.org/docs/manifest/processors/#the-otel-metrics-section" },
                { "ParseAs", "https://mqtt2otel.org/docs/manifest/processors/#parseas" },
                { "Open telemetry logs", "https://mqtt2otel.org/docs/manifest/processors/#the-otel-logs-section" },
                { "Expressions", "https://mqtt2otel.org/docs/expressions/"},
                { "Supported functions", "https://mqtt2otel.org/docs/expressions/#available-functions" },
                { "Expression constants", "https://mqtt2otel.org/docs/expressions/#constants" },
                { "Transformations", "https://mqtt2otel.org/docs/expressions/#transformations" },
                { "Extended dissect parser", "https://github.com/OSgAgA/Dissect.Extended.Net" },
                { "GROK parser", "https://www.elastic.co/docs/reference/logstash/plugins/plugins-filters-grok" },
                { "TopicAttribute", "https://mqtt2otel.org/docs/expressions/topicparsing/#the-topicattribute-syntax" },
                { "TopicPath", "https://mqtt2otel.org/docs/expressions/topicparsing/#the-topicpath-syntax" },
                { "User properties", "https://mqtt2otel.org/docs/expressions/userproperties" },
                { "Converter and Formatters", "https://mqtt2otel.org/docs/expressions/converter-and-formatter/" },
                { "Metric actions", "https://mqtt2otel.org/docs/expressions/actions/" },
                { "Instrumentation Scope", "https://mqtt2otel.org/docs/manifest/scopes/" },
                { "Mappings", "https://mqtt2otel.org/docs/manifest/mappings/" },
            };
        }
    }
}
