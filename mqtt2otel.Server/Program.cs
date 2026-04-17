using mqtt2otel.Interfaces;
using mqtt2otel.InternalLogging;
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

            // Using host builder as currently no web functionality is used.
            // Change to WebApplication if endpoints, dontrollers and so on are needed.
            var builder = Host.CreateApplicationBuilder(args);
            builder.Logging.ClearProviders();
            builder.Services.AddHostedService<Mqtt2OtelService>();

            builder.Services.AddSingleton<ApplicationSettings>(appSettings);
            builder.Services.AddSingleton<ILoggerFactory>(logFactory);
            builder.Services.AddSingleton<ISignalStore, SignalStore>();
            builder.Services.AddSingleton<ILoggerStore, LoggerStore>();
            builder.Services.AddSingleton<IPayloadParser, PayloadParser>();
            builder.Services.AddSingleton<IPayloadTransformation, PayloadTransformation>();
            builder.Services.AddSingleton<IObjectFactory, Manifest.ObjectFactory>();
            builder.Services.AddSingleton<IManifestCoordinator, ManifestCoordinator>();
            builder.Services.AddSingleton<IOtelCoordinator, OtelCoordinator>();
            builder.Services.AddSingleton<IMqttCoordinator, MqttCoordinator>();
            builder.Services.AddSingleton<Bootstrapper>();
            builder.Services.AddSingleton<IDataStores, DataStores>();

            var app = builder.Build();

            app.Run();
        }
    }
}
