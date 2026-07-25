using Microsoft.Extensions.Options;
using mqtt2otel.ManifestExplorer.Client.Pages;
using mqtt2otel.ManifestExplorer.Components;
using mqtt2otel.ManifestExplorer.Meters;
using mqtt2otel.ManifestExplorer.Settings;
using OpenTelemetry;
using OpenTelemetry.Metrics;

namespace mqtt2otel.ManifestExplorer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();
            //.AddInteractiveWebAssemblyComponents();

            builder.Services.AddControllers();
            builder.Services.AddBlazorBootstrap();

            builder.Services.Configure<ServerApiOptions>(builder.Configuration.GetSection("ServerApi"));
            builder.Services.Configure<FeatureToggles>(builder.Configuration.GetSection("FeatureToggles"));

            var filePath = Path.Combine(builder.Environment.ContentRootPath, "App_Data", "version.txt");
            builder.Services.AddSingleton<ApplicationInfo>(new ApplicationInfo(filePath ?? string.Empty));
            builder.Services.AddSingleton<UsageMeter>();

            builder.Services.AddHttpClient("ServerAPI", (serviceProvider, client) =>
            {
                var opts = serviceProvider.GetRequiredService<IOptions<ServerApiOptions>>().Value;
                client.BaseAddress = new Uri(opts.BaseAddress);
            });

            var otelSettings = builder.Configuration.GetSection("Otel").Get<OtelSettings>();

            if (otelSettings != null)
            {
                builder.Services.AddOpenTelemetry()
                         .WithMetrics(metrics =>
                         {
                             metrics.AddMeter(nameof(UsageMeter));

                             metrics.AddOtlpExporter(options =>
                             {
                                 options.Endpoint = new Uri(otelSettings.Endpoint.Uri);
                                 options.Protocol = otelSettings.OtlpExportProtocol;
                                 options.ExportProcessorType = otelSettings.ExportProcessorType;
                             });
                         });
            }

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseWebAssemblyDebugging();
            }
            else
            {
                app.UseExceptionHandler("/Error");
            }

            app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
            app.UseAntiforgery();

            app.MapStaticAssets();
            app.MapControllers();
            app.MapRazorComponents<App>()
                //.AddInteractiveWebAssemblyRenderMode()
                .AddInteractiveServerRenderMode()
                .AddAdditionalAssemblies(typeof(Client._Imports).Assembly);

            app.Run();
        }
    }
}
