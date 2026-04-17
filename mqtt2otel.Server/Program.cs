using mqtt2otel.InternalLogging;
using mqtt2otel.Parser;
using mqtt2otel.Stores;
using mqtt2otel.Transformation;

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
            builder.Services.AddSingleton<SignalStore>();
            builder.Services.AddSingleton<LoggerStore>();
            builder.Services.AddSingleton<PayloadParser>();
            builder.Services.AddSingleton<PayloadTransformation>();
            builder.Services.AddSingleton<Manifest.ObjectFactory>();
            builder.Services.AddSingleton<ManifestCoordinator>();
            builder.Services.AddSingleton<OtelCoordinator>();
            builder.Services.AddSingleton<MqttCoordinator>();
            builder.Services.AddSingleton<Bootstrapper>();
            builder.Services.AddSingleton<DataStores>();

            var app = builder.Build();

            app.Run();
        }
    }
}
