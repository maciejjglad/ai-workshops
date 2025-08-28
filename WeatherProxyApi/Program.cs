using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Azure.Functions.Worker.Extensions.OpenApi.Extensions;
using Serilog;
using WeatherProxyApi.Extensions;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(outputTemplate: 
        "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .CreateLogger();

try
{
    Log.Information("Starting Weather Proxy API");

    var host = new HostBuilder()
        .ConfigureFunctionsWorkerDefaults()
        .ConfigureOpenApi()
        .ConfigureServices(services =>
        {
            // Add Serilog
            services.AddSerilog();

            // Add Weather API services
            services.AddWeatherApiServices();
            
            // Add HTTP clients with resilience
            services.AddWeatherApiHttpClients();
            
            // Add CORS
            services.AddWeatherApiCors();
        })
        .UseSerilog()
        .Build();

    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Weather Proxy API terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
