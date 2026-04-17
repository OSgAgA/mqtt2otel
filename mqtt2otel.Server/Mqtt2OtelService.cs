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
        /// The bootstrapper for bootstrapping the application.
        /// </summary>
        private Bootstrapper Bootstrapper;

        /// <summary>
        /// Initializes a new instance of the <see cref="Mqtt2OtelService"/> class.
        /// </summary>
        /// <param name="bootstrapper">The bootstrapper for bootstrapping the application.</param>
        public Mqtt2OtelService(Bootstrapper bootstrapper)
        {
            this.Bootstrapper = bootstrapper;
        }

        /// <summary>
        /// Executes the application.
        /// </summary>
        /// <param name="stoppingToken">A cancelation token for stopping the service.</param>
        /// <returns></returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Bootstrapper.Bootstrap();

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}
