using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Options;
using mqtt2otel.Helper;
using mqtt2otel.Interfaces;
using mqtt2otel.InternalLogging;
using mqtt2otel.InternalMetrics;
using mqtt2otel.Parser;
using mqtt2otel.Stores;
using mqtt2otel.Transformation;
using YamlDotNet.Serialization;

namespace mqtt2otel.Server
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var appSettings = Bootstrapper.ReadApplicationSettings();
            var logFactory = Bootstrapper.InitializeLogFactory(appSettings.Logging);

            var builder = WebApplication.CreateBuilder(args);

            builder.Logging.ClearProviders();
            builder.Services.AddHostedService<Mqtt2OtelService>();

            builder.Services.AddSingleton<ApplicationSettings>(appSettings);
            builder.Services.AddSingleton<ILoggerFactory>(logFactory);
            builder.Services.AddSingleton<ISignalStore, SignalStore>();
            builder.Services.AddSingleton<ILoggerStore, LoggerStore>();
            builder.Services.AddSingleton<IPayloadParser, PayloadParser>();
            builder.Services.AddSingleton<IPayloadTransformation, PayloadTransformation>();
            builder.Services.AddSingleton<IEmbeddedExpressionParser, EmbeddedExpressionParser>();
            builder.Services.AddSingleton<IObjectFactory, Manifest.ObjectFactory>();
            builder.Services.AddSingleton<IManifestCoordinator, ManifestCoordinator>();
            builder.Services.AddSingleton<IOtelCoordinator, OtelCoordinator>();
            builder.Services.AddSingleton<IMqttCoordinator, MqttCoordinator>();
            builder.Services.AddSingleton<Bootstrapper>();
            builder.Services.AddSingleton<IDataStores, DataStores>();
            builder.Services.AddSingleton<IOtelExporterBuilder, OtelExporterBuilder>();
            builder.Services.AddSingleton<MqttMeter>(new MqttMeter());
            builder.Services.AddSingleton<OtelInternalMeter>(new OtelInternalMeter());
            builder.Services.AddSingleton<ManifestMeter>(new ManifestMeter());
            builder.Services.AddSingleton<ProcessorMeter>(new ProcessorMeter());

            builder.Services.AddHttpClient("ServerAPI", (serviceProvider, client) =>
            {
                client.BaseAddress = new Uri(appSettings.BaseAddress);
            });

            builder.Services.AddControllers();
            builder.WebHost.UseUrls(appSettings.BaseAddress);


            var app = builder.Build();

            app.MapControllers();

            app.Run();
        }
    }
}
