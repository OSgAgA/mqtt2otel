using Microsoft.AspNetCore.Server.Kestrel.Https;
using System.Security.Cryptography.X509Certificates;

namespace mqtt2otel.Server
{
    public class Mqtt2OtelService : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Bootstrapper.Bootstrap();

            while (!stoppingToken.IsCancellationRequested)
            {
                // your main loop
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}
