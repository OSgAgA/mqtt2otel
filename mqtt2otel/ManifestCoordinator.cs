using Microsoft.Extensions.Logging;
using mqtt2otel.Interfaces;
using mqtt2otel.InternalLogging;
using mqtt2otel.Manifest;
using mqtt2otel.Stores;
using System;
using System.Collections.Generic;
using System.Text;

namespace mqtt2otel
{
    /// <summary>
    /// Coordinates all efforts regarding loading, unloading and updating of manifests.
    /// </summary>
    public class ManifestCoordinator : IManifestCoordinator
    {
        /// <summary>
        /// The otel coordinator used for managing all open telemetry connections.
        /// </summary>
        private IOtelCoordinator otelCoordinator;

        /// <summary>
        /// The data stores used by the application to exchange data asynchronously.
        /// </summary>
        private IDataStores dataStores;

        /// <summary>
        /// The mqtt broker coordinater that manages all mqtt connections.
        /// </summary>
        private IMqttCoordinator mqttCoordinator;

        /// <summary>
        /// The logger used for internal logging.
        /// </summary>
        private ILogger internalLogger;

        /// <summary>
        /// The application settings.
        /// </summary>
        private ApplicationSettings applicationSettings;

        /// <summary>
        /// The last time the manifest file has changed.
        /// </summary>
        private DateTime? LastManifestFileChange;

        /// <summary>
        /// Initializes a new instance of the <see cref="Bootstrapper"/> class.
        /// 
        /// </summary>
        /// <param name="internalLogger">The internal logger.</param>
        /// <param name="otelCoordinator">The otel coordinator used for communicating with an otel endpoint.</param>
        /// <param name="mqttCoordinator">The mqtt coordinator for communicating with a mqtt broker.</param>
        /// <param name="dataStores">The data stores used by the application to exchange data asynchronously.</param>
        /// <param name="loggerStore">The logger store for providing otel loggers to consumers.</param>
        public ManifestCoordinator(ILogger<Bootstrapper> internalLogger, IOtelCoordinator otelCoordinator, IMqttCoordinator mqttCoordinator, IDataStores dataStores, ApplicationSettings applicationSettings)
        {
            this.applicationSettings = applicationSettings;
            this.otelCoordinator = otelCoordinator;
            this.dataStores = dataStores;
            this.mqttCoordinator = mqttCoordinator;
            this.internalLogger = internalLogger;
        }

        /// <summary>
        /// Registers the configures autoupdate for reloading the manifest file on changes.
        /// </summary>
        public void RegisterAutoUpdater()
        {
            var fullPath = Path.GetFullPath(this.applicationSettings.ManifestPath) ?? "./";

            this.internalLogger.LogInformation($"Watching for manifest changes at {fullPath}.");
            this.internalLogger.LogInformation($"Polling intervall for manifest updates is set to {this.applicationSettings.PollIntervallInSeconds}s.");

            this.LastManifestFileChange = File.GetLastWriteTimeUtc(this.applicationSettings.ManifestPath);
            _ = this.MonitorFileAsync(this.applicationSettings.ManifestPath, this.applicationSettings.PollIntervallInSeconds);
        }

        /// <summary>
        /// Dispose all existing connections to the mqtt brokers and otel endpoints.
        /// </summary>
        public void DisposeConnections()
        {
            if (this.mqttCoordinator != null) this.mqttCoordinator.DisconnectAllBrokers().Wait();
            if (this.otelCoordinator != null) this.otelCoordinator.Dispose();
        }

        /// <summary>
        /// Process the manifest file.
        /// </summary>
        /// <returns>A value indicating whether the operation has been successfull.</returns>
        public async Task<bool> ProcessManifest()
        {
            var manifest = new Manifest.Manifest();
            try
            {
                manifest = Manifest.Manifest.ReadFromYaml(this.internalLogger, this.applicationSettings.ManifestPath);
            }
            catch (Exception ex)
            {
                var message = ex.ToString();

                if (ex.InnerException != null)
                {
                    message = $"{message}: {ex.InnerException.Message}";
                }

                this.internalLogger.LogCritical($"Error parsing manifest file. {message}");
                return false;
            }

            manifest.Initialize();

            var validationResult = manifest.Validate();

            validationResult.LogOutput(this.internalLogger);

            if (validationResult.Success == false)
            {
                this.internalLogger.LogCritical("Could not validate manifest. Shutting down application.");
                return false;
            }

            this.dataStores.SignalStore.DeleteStore();
            this.dataStores.LoggerStore.DeleteStore();

            if (this.otelCoordinator == null)
            {
                this.internalLogger.LogCritical("Internal error: OtelCoordinator is not set!");
                return false;
            }

            if (this.mqttCoordinator == null)
            {
                this.internalLogger.LogCritical("Internal error: MqttCoordinator is not set!");
                return false;
            }

            this.otelCoordinator.Connect(manifest);

            await mqttCoordinator.ConnectAndSubscribe(manifest);
            return true;
        }

        /// <summary>
        /// Reload the manifest file.
        /// </summary>
        private async Task ReloadManifest()
        {
            this.internalLogger.LogInformation("Reloading manifest file..");
            using (this.internalLogger.StartActivity("Reload manifest."))
            {
                this.DisposeConnections();
                await this.ProcessManifest();
            }
            this.internalLogger.LogInformation("Reloading manifest completed.");
        }

        /// <summary>
        /// Periodically polls for changes of the manifest file.
        /// </summary>
        /// <param name="path">The path to the manifest file.</param>
        /// <param name="intervalInSeconds">The time intervall in seconds.</param>
        private async Task MonitorFileAsync(string path, int intervalInSeconds)
        {
            var timer = new PeriodicTimer(TimeSpan.FromSeconds(intervalInSeconds));

            while (await timer.WaitForNextTickAsync())
            {
                var lastWrite = File.GetLastWriteTimeUtc(path);

                if (lastWrite != this.LastManifestFileChange)
                {
                    this.LastManifestFileChange = lastWrite;
                    await ReloadManifest();
                }
            }
        }
    }
}
