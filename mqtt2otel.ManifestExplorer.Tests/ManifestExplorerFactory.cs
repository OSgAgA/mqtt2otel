using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Options;
using mqtt2otel.ManifestExplorer.Settings;

namespace mqtt2otel.ManifestExplorer.Tests
{
    /// <summary>
    /// Responsible for starting up the mqtt2otel manifest explorer for testing.
    /// </summary>
    public class ManifestExplorerFactory : WebApplicationFactory<mqtt2otel.ManifestExplorer.Program>
    {
        /// <summary>
        /// Holds the kestrel host.
        /// </summary>
        IHost? kestrelHost;

        /// <summary>
        /// Used for temporary storing the server address.
        /// </summary>
        readonly AddressBox addressBox = new();

        /// <summary>
        /// Creates the host for testing.
        /// </summary>
        /// <param name="builder">The host builder</param>
        /// <returns>The created host.</returns>
        protected override IHost CreateHost(IHostBuilder builder)
        {
            // Registered on the builder BEFORE either host is built, so it
            // applies to both the TestServer host and the Kestrel host that
            // get constructed from it.
            builder.ConfigureServices(services =>
            {
                services.PostConfigure<ServerApiOptions>(opts =>
                {
                    if (addressBox.BaseAddress is not null)
                        opts.BaseAddress = addressBox.BaseAddress;
                });
            });

            // Build the in-memory TestServer host first; the base class
            // insists on getting one back.
            var testHost = builder.Build();

            // Build a second host, this time on real Kestrel.
            builder.ConfigureWebHost(b => b.UseKestrel()
                                           .UseUrls("http://127.0.0.1:0"));
            this.kestrelHost = builder.Build();
            this.kestrelHost.Start();

            var server = kestrelHost.Services.GetRequiredService<IServer>();
            var addresses = server.Features.Get<IServerAddressesFeature>();

            var url = addresses!.Addresses
                .Select(x => new Uri(x))
                .Last();

            addressBox.BaseAddress = url.ToString();          // fills the box *before* anything resolves the options
            ClientOptions.BaseAddress = url;

            testHost.Start();
            return testHost;
        }

        /// <summary>
        /// Gets the adress of the created server as a string.
        /// </summary>
        public string ServerAddress
        {
            get
            {
                EnsureServer();
                return ClientOptions.BaseAddress.ToString();
            }
        }

        /// <summary>
        /// Ensures that the server is available. Creates it if necessary.
        /// </summary>
        private void EnsureServer()
        {
            if (this.kestrelHost is null)
            {
                // This forces WebApplicationFactory to bootstrap the server  
                using var _ = CreateDefaultClient();
            }
        }

        /// <summary>
        /// Needed for temporarely storing the server address.
        /// </summary>
        private class AddressBox { public string? BaseAddress; }

    }
}
