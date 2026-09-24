using Microsoft.Testing.Extensions;
using Microsoft.Testing.Platform.Builder;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace mqtt2otel.ManifestExplorer.Tests
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = await TestApplication.CreateBuilderAsync(args);

            builder.AddOpenTelemetryProvider(
                withTracing: tracing => tracing
                    .AddTestingPlatformInstrumentation()
                    .AddOtlpExporter(),
                withMetrics: metrics => metrics
                    .AddTestingPlatformInstrumentation()
                    .AddOtlpExporter());

            using var app = await builder.BuildAsync();

            await app.RunAsync();
        }
    }
}
