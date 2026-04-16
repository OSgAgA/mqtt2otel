using Microsoft.Extensions.Logging;
using mqtt2otel.Manifest;
using mqtt2otel.InternalLogging;
using mqtt2otel.Parser;
using mqtt2otel.Stores;
using mqtt2otel.Transformation;

namespace mqtt2otel
{
    public class Bootstrapper
    {
        /// <summary>
        /// The otel coordinator used for managing all open telemetry connections.
        /// </summary>
        private static OtelCoordinator? otelCoordinator;

        /// <summary>
        /// The central signal store for exchanging signal messages between the otel coordinator and
        /// the mqtt coordinator.
        /// </summary>
        private static SignalStore? signalStore;

        /// <summary>
        /// The central logging store for exchanging loggers between the otel coordinator and the
        /// mqtt coordinator.
        /// </summary>
        private static LoggerStore? loggerStore;

        /// <summary>
        /// The mqtt broker coordinater that manages all mqtt connections.
        /// </summary>
        private static MqttCoordinator? mqttCoordinator;

        /// <summary>
        /// The logger factory used for creating internal loggers.
        /// </summary>
        private static ILoggerFactory internalLogFactory = InternalLogFactory.Create(new InternalLoggingSettings());

        /// <summary>
        /// A filesystem watcher used for monitoring changes on the manifest file.
        /// </summary>
        private static FileSystemWatcher? watcher;

        /// <summary>
        /// The application settings.
        /// </summary>
        private static ApplicationSettings applicationSettings = new();

        /// <summary>
        /// The last time the manifest file has changed.
        /// </summary>
        private static DateTime? LastManifestFileChange;

        /// <summary>
        /// The factory for creating the manifest via the yaml parser.
        /// </summary>
        private static ObjectFactory? objectFactory;

        /// <summary>
        /// Reads the application settings file.
        /// </summary>
        /// <returns>The application settings.</returns>
        /// <exception cref="Exception">Thrown when application settings file could not be parsed.</exception>
        public static ApplicationSettings ReadApplicationSettings()
        {
            // Default path for application settings.
            string applicationSettingsPath = "/config/ApplicationSettings.yaml";

            // Look directly in the application directory, useful for development
            if (!Path.Exists(applicationSettingsPath)) applicationSettingsPath = "./ApplicationSettings.yaml";

            // If it does not exist, just take the default settings.
            if (Path.Exists(applicationSettingsPath))
            {
                try
                {
                    Bootstrapper.applicationSettings = ApplicationSettings.ReadFromYaml(applicationSettingsPath);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"CRITICAL: Could not read {applicationSettingsPath}. The following error occured: {ex.ToString()}.");
                    Console.WriteLine("CRITICAL: Shutting down application.");

                    throw new Exception();
                }
            }

            return Bootstrapper.applicationSettings;
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
                Bootstrapper.internalLogFactory = InternalLogFactory.Create(settings);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"CRITICAL: Could not initialize the internal logging factory. The following error occured: {ex.ToString()}. Shutting down application.");
                Console.WriteLine("CRITICAL: Shutting down application.");

                throw new Exception();
            }

            return Bootstrapper.internalLogFactory;
        }

        /// <summary>
        /// Bootstraps the applicatino.
        /// </summary>
        /// <<param name="objectFactory">The factory for creating the manifest via the yaml parser.</param>
        /// <param name="signalStore">The store for otel signals.</param>
        /// <param name="loggerStore">The store for otel loggers.</param>
        /// <returns>A return code.</returns>
        public static async Task<int> Bootstrap(ObjectFactory objectFactory, SignalStore signalStore, LoggerStore loggerStore)
        {
            var internalLogger = Bootstrapper.internalLogFactory.CreateLogger<Bootstrapper>();
            internalLogger.LogInformation("ApplicationSettings.yaml read.");

            Bootstrapper.signalStore = signalStore;
            Bootstrapper.loggerStore = loggerStore;
            Bootstrapper.objectFactory = objectFactory;

            (bool flowControl, int value) = await ProcessManifest(Bootstrapper.internalLogFactory, objectFactory);

            if (!flowControl)
            {
                return value;
            }

            Bootstrapper.RegisterAutoUpdater();

            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true; // prevent immediate termination

                var internalLogger = Bootstrapper.internalLogFactory.CreateLogger<Bootstrapper>();

                internalLogger.LogInformation("Shutting down application...");
                using (internalLogger.StartActivity("Shutting down application."))
                {
                    Bootstrapper.DisposeConnections();
                }
                internalLogger.LogInformation("Shutdown completed. Good bye.");

                Environment.Exit(0);
            };

            internalLogger.LogInformation("Application up and running!");

            return 0;
        }

        /// <summary>
        /// Registers the configures autoupdate for reloading the manifest file on changes.
        /// </summary>
        private static void RegisterAutoUpdater()
        {
            if (Bootstrapper.applicationSettings.AutoUpdateMode == AutoUpdateMode.WatchManifestFile)
            {
                string dir = Path.GetDirectoryName(Bootstrapper.applicationSettings.ManifestPath) ?? "./";
                string filename = Path.GetFileName(Bootstrapper.applicationSettings.ManifestPath);

                Bootstrapper.watcher = new FileSystemWatcher(dir, filename)
                {
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName,
                    IncludeSubdirectories = false,
                    EnableRaisingEvents = true
                };

                watcher.Changed += async (_, __) => await ReloadManifest();
            }
            else if (Bootstrapper.applicationSettings.AutoUpdateMode == AutoUpdateMode.PollManifestFile)
            {
                Bootstrapper.LastManifestFileChange = File.GetLastWriteTimeUtc(Bootstrapper.applicationSettings.ManifestPath);
                _ = Bootstrapper.MonitorFileAsync(Bootstrapper.applicationSettings.ManifestPath, Bootstrapper.applicationSettings.PollIntervallInSeconds);
            }
        }

        /// <summary>
        /// Dispose all existing connections to the mqtt brokers and otel endpoints.
        /// </summary>
        private static void DisposeConnections()
        {
            if (Bootstrapper.mqttCoordinator != null) Bootstrapper.mqttCoordinator.DisconnectAllBrokers().Wait();
            if (Bootstrapper.otelCoordinator != null) Bootstrapper.otelCoordinator.Dispose();
        }

        /// <summary>
        /// Process the manifest file.
        /// </summary>
        /// <param name="internalLogFactory">The internal logger.</param>
        /// <param name="objectFactory">The factory used for creating the manifest via the yaml parser.</param>
        /// <returns>A value indicating whether the operation has been successfull and an error code.</returns>
        private static async Task<(bool flowControl, int value)> ProcessManifest(ILoggerFactory internalLogFactory, ObjectFactory objectFactory)
        {
            if (Bootstrapper.signalStore == null) throw new Exception("Signal store not set.");
            if (Bootstrapper.loggerStore == null) throw new Exception("Logger store not set.");

            var manifest = new Manifest.Manifest();
            try
            {
                manifest = Manifest.Manifest.ReadFromYaml(objectFactory, internalLogFactory, Bootstrapper.applicationSettings.ManifestPath);
            }
            catch (Exception ex)
            {
                var message = ex.ToString();

                if (ex.InnerException != null)
                {
                    message = $"{message}: {ex.InnerException.Message}";
                }

                internalLogFactory.CreateLogger<Manifest.Manifest>().LogCritical($"Error parsing manifest file. {message}");
                internalLogFactory.Dispose();
                return (flowControl: false, value: -3);
            }

            manifest.Initialize();

            var validationResult = manifest.Validate();

            validationResult.LogOutput(internalLogFactory.CreateLogger<ValidationResult>());

            if (validationResult.Success == false)
            {
                internalLogFactory.CreateLogger<Manifest.Manifest>().LogCritical("Could not validate manifest. Shutting down application.");
                internalLogFactory.Dispose();
                return (flowControl: false, value: -4);
            }

            Bootstrapper.signalStore.DeleteStore();
            Bootstrapper.loggerStore.DeleteStore();

            Bootstrapper.otelCoordinator = new OtelCoordinator(internalLogFactory.CreateLogger<OtelCoordinator>(), manifest, Bootstrapper.signalStore, Bootstrapper.loggerStore);

            var payloadTransformation = new PayloadTransformation();
            var payloadParser = new PayloadParser();

            Bootstrapper.mqttCoordinator = new MqttCoordinator(internalLogFactory.CreateLogger<MqttCoordinator>(), Bootstrapper.signalStore, Bootstrapper.loggerStore, payloadParser, payloadTransformation);

            await mqttCoordinator.ConnectAndSubscribe(manifest);
            return (flowControl: true, value: default);
        }

        /// <summary>
        /// Reload the manifest file.
        /// </summary>
        private async static Task ReloadManifest()
        {
            var internalLogger = Bootstrapper.internalLogFactory.CreateLogger<Bootstrapper>();

            if (objectFactory == null)
            {
                internalLogger.LogError("Internal error: Cannot relaod manifest as the object factory is null.");
                return;
            }

            internalLogger.LogInformation("Reloading manifest file..");
            using (internalLogger.StartActivity("Reload manifest."))
            {
                Bootstrapper.DisposeConnections();
                await Bootstrapper.ProcessManifest(Bootstrapper.internalLogFactory, objectFactory);
            }
            internalLogger.LogInformation("Reloading manifest completed.");
        }

        /// <summary>
        /// Periodically polls for changes of the manifest file.
        /// </summary>
        /// <param name="path">The path to the manifest file.</param>
        /// <param name="intervalInSeconds">The time intervall in seconds.</param>
        private static async Task MonitorFileAsync(string path, int intervalInSeconds)
        {
            var timer = new PeriodicTimer(TimeSpan.FromSeconds(intervalInSeconds));

            while (await timer.WaitForNextTickAsync())
            {
                var lastWrite = File.GetLastWriteTimeUtc(path);

                if (lastWrite != Bootstrapper.LastManifestFileChange)
                {
                    Bootstrapper.LastManifestFileChange = lastWrite;
                    await ReloadManifest();
                }
            }
        }

    }
}