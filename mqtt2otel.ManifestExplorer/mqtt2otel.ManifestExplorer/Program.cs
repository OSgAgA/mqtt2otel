using mqtt2otel.ManifestExplorer.Client.Pages;
using mqtt2otel.ManifestExplorer.Components;

namespace mqtt2otel.ManifestExplorer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();
                //.AddInteractiveWebAssemblyComponents();

            builder.Services.AddControllers();
            builder.Services.AddBlazorBootstrap();

            builder.Services.AddHttpClient("ServerAPI", client =>
            {
                client.BaseAddress = new Uri("http://localhost:5197/"); // adjust to your server
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseWebAssemblyDebugging();
            }
            else
            {
                app.UseExceptionHandler("/Error");
            }

            app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
            app.UseAntiforgery();

            app.MapStaticAssets();
            app.MapControllers();
            app.MapRazorComponents<App>()
                //.AddInteractiveWebAssemblyRenderMode()
                .AddInteractiveServerRenderMode()
                .AddAdditionalAssemblies(typeof(Client._Imports).Assembly);

            app.Run();
        }
    }
}
