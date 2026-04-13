namespace mqtt2otel.Server
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Logging.ClearProviders();
            builder.Services.AddHostedService<Mqtt2OtelService>();
            var app = builder.Build();

            app.Run();
        }
    }
}
