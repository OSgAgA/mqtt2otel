using Microsoft.AspNetCore.Server.Kestrel.Https;
using mqtt2otel.Stores;
using System.Security.Cryptography.X509Certificates;

namespace mqtt2otel.Server
{
    /// <summary>
    /// Creates a a background service for the application.
    /// </summary>
    public class Mqtt2OtelService : BackgroundService
    {
        /// <summary>
        /// The object factory used for creating the manifest from the yaml file.
        /// </summary>
        private Manifest.ObjectFactory objectFactory;

        /// <summary>
        /// The store for otel signals.
        /// </summary>
        private SignalStore signalStore;

        /// <summary>
        /// The store for otel loggers.
        /// </summary>
        private LoggerStore loggerStore;

        /// <summary>
        /// Initializes a new instance of the <see cref="Mqtt2OtelService"/> class.
        /// </summary>
        /// <param name="objectFactory">The object factory used for creating the manifest from the yaml file.</param>
        /// <param name="signalStore">The store for otel signals.</param>
        /// <param name="loggerStore">The store for otel loggers.</param>
        public Mqtt2OtelService(Manifest.ObjectFactory objectFactory, SignalStore signalStore, LoggerStore loggerStore)
        {
            this.objectFactory = objectFactory;
            this.signalStore = signalStore;
            this.loggerStore = loggerStore;
        }

        /// <summary>
        /// Executes the application.
        /// </summary>
        /// <param name="stoppingToken">A cancelation token for stopping the service.</param>
        /// <returns></returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Bootstrapper.Bootstrap(this.objectFactory, this.signalStore, this.loggerStore);

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}
