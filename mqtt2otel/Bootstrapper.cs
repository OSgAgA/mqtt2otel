using Microsoft.Extensions.Logging;
using mqtt2otel.Interfaces;
using mqtt2otel.InternalLogging;
using mqtt2otel.Manifest;
using mqtt2otel.Parser;
using mqtt2otel.Stores;
using mqtt2otel.Transformation;
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
        public Bootstrapper(ILogger<Bootstrapper> internalLogger, IManifestCoordinator manifestCoordinator, IObjectFactory objectFactory)
        {
            this.internalLogger = internalLogger;
            this.manifestCoordinator = manifestCoordinator;
            Manifest.Manifest.ObjectFactory = objectFactory;
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

            // Look directly in the application directory, useful for development
            if (!Path.Exists(applicationSettingsPath)) applicationSettingsPath = "./ApplicationSettings.yaml";

            // If it does not exist, just take the default settings.
            if (Path.Exists(applicationSettingsPath))
            {
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

            return new ApplicationSettings();
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
        /// Bootstraps the applicatino.
        /// </summary>
        /// <returns>A return code.</returns>
        public async Task<int> Bootstrap()
        {
            this.internalLogger.LogInformation("ApplicationSettings.yaml read.");

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

            bool success = await this.manifestCoordinator.ProcessManifest();

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