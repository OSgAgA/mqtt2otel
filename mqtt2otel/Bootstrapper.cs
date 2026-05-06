using Microsoft.Extensions.Logging;
using mqtt2otel.Interfaces;
using mqtt2otel.InternalLogging;
using mqtt2otel.InternalMetrics;
using mqtt2otel.Manifest;
using mqtt2otel.Parser;
using mqtt2otel.Stores;
using mqtt2otel.Transformation;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using YamlDotNet.Serialization;

namespace mqtt2otel
{
    public class Bootstrapper
    {
        /// <summary>
        /// The logger used for internal logging.
        /// </summary>
        private ILogger internalLogger;

        /// <summary>
        /// The manifest coordinator used for loading, unloading and reloading of manifests files.
        /// </summary>
        private IManifestCoordinator manifestCoordinator;

        /// <summary>
        /// The application settings.
        /// </summary>
        private ApplicationSettings applicationSettings;

        /// <summary>
        /// The internally used meter provider.
        /// </summary>
        private MeterProvider? meterProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="Bootstrapper"/> class.
        /// 
        /// Bootstrapping the application should be done in the following order:
        /// 
        ///   1. <see cref="ReadApplicationSettings"/>
        ///   2. <see cref="InitializeLogFactory"/>
        ///   3. Create new instance of <see cref="Bootstrapper"/>
        ///   4. <see cref="Bootstrap"/>
        ///   
        /// </summary>
        /// <param name="internalLogger">The logger used for internal logging.</param>
        /// <param name="manifestCoordinator">The manifest coordinator used for loading, unloading and reloading of manifests files.</param>
        /// <param name="objectFactory">The object factory for creating objects from a yaml file.</param>
        /// <param name="settings">The application settings.</param>
        /// <param name="exporterBuilder">The exporter builder for connecting the metrics to an otel exporter.</param>
        public Bootstrapper(ILogger<Bootstrapper> internalLogger, IManifestCoordinator manifestCoordinator, IObjectFactory objectFactory, ApplicationSettings settings, IOtelExporterBuilder exporterBuilder)
        {
            this.internalLogger = internalLogger;
            this.applicationSettings = settings;
            this.manifestCoordinator = manifestCoordinator;
            Manifest.Manifest.ObjectFactory = objectFactory;
            this.InitializeInternalMeters(exporterBuilder, settings);
        }

        /// <summary>
        /// Reads the application settings file.
        /// </summary>
        /// <returns>The application settings.</returns>
        /// <exception cref="Exception">Thrown when application settings file could not be parsed.</exception>
        public static ApplicationSettings ReadApplicationSettings()
        {
            // Default path for application settings.
            string applicationSettingsPath = "/config/ApplicationSettings.yaml";

            try
            {
                return ApplicationSettings.ReadFromYaml(applicationSettingsPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"CRITICAL: Could not read {applicationSettingsPath}. The following error occured: {ex.ToString()}.");
                Console.WriteLine("CRITICAL: Shutting down application.");

                throw new Exception();
            }
        }

        /// <summary>
        /// Initializes the log factory, used for internal logging.
        /// </summary>
        /// <param name="settings">The settings for creating the factory.</param>
        /// <exception cref="Exception">Thrown when the log factory cannot be initialized.</exception>
        public static ILoggerFactory InitializeLogFactory(InternalLoggingSettings settings)
        {
            try
            {
                var internalLogFactory = InternalLogFactory.Create(settings);
                return internalLogFactory;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"CRITICAL: Could not initialize the internal logging factory. The following error occured: {ex.ToString()}. Shutting down application.");
                Console.WriteLine("CRITICAL: Shutting down application.");

                throw new Exception();
            }
        }

        /// <summary>
        /// Initializes all supported internal otel meters if otel logging is configured in the application settings.
        /// </summary>
        /// <param name="exporterBuilder">The exporter builder for configuring an exporter for the metrics.</param>
        /// <param name="settings">The application settings.</param>
        public void InitializeInternalMeters(IOtelExporterBuilder exporterBuilder, ApplicationSettings settings)
        {
            if (settings.Metrics.Otel == null || !settings.Metrics.CollectMetrics) return;

            var builder = Sdk.CreateMeterProviderBuilder()
                    .SetResourceBuilder(
                            ResourceBuilder.CreateDefault()
                            .AddService(settings.Metrics.Otel.ServiceName, serviceNamespace: settings.Metrics.Otel.ServiceNamespace, serviceVersion: settings.Metrics.Otel.ServiceVersion))
                    .AddMeter(nameof(MqttMeter))
                    .AddMeter(nameof(ProcessorMeter))
                    .AddMeter(nameof(ManifestMeter))
                    .AddMeter(nameof(OtelMeter));

            exporterBuilder.AddToMeterProviderBuilder(builder, settings.Metrics.Otel);

            this.meterProvider = builder.Build();
        }

        /// <summary>
        /// Bootstraps the applicatino.
        /// </summary>
        /// <returns>A return code.</returns>
        public async Task<int> Bootstrap()
        {
            // Read version number file or set to not defined, if no file is found.
            string version = "Not defined";
            string versionFilePath = "./version.txt";
            if (Path.Exists(versionFilePath))
            {
                var lines = File.ReadLines(versionFilePath);
                if (lines.Count() > 0) version = lines.First();
            }

            this.internalLogger.LogInformation($"Starting application with version: {version}");
            this.internalLogger.LogInformation("ApplicationSettings.yaml read.");

            // Register ctrl-c
            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true; // prevent immediate termination

                this.internalLogger.LogInformation("Shutting down application...");
                using (this.internalLogger.StartActivity("Shutting down application."))
                {
                    this.manifestCoordinator.DisposeConnections();
                }
                this.internalLogger.LogInformation("Shutdown completed. Good bye.");

                Environment.Exit(0);
            };

            bool success = false;

            // Process the manifest file.
            try
            {
                success = await this.manifestCoordinator.ProcessManifest();
            }
            catch (Exception ex)
            {
                internalLogger.LogCritical(ex, "Error on starting up application. Shutting down.");
                return -1;
            }

            this.manifestCoordinator.RegisterAutoUpdater();

            if (!success)
            {
                return -1;
            }

            this.internalLogger.LogInformation("Application up and running!");

            return 0;
        }
    }
}