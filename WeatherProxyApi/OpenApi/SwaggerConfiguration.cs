using Microsoft.Azure.Functions.Worker.Extensions.OpenApi.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;

namespace WeatherProxyApi.OpenApi;

public static class SwaggerConfiguration
{
    public static IHostBuilder ConfigureOpenApi(this IHostBuilder builder)
    {
        builder.ConfigureOpenApi(options =>
        {
            options.AddOpenApiConfigurationOptions(new OpenApiConfigurationOptions
            {
                Info = new OpenApiInfo
                {
                    Version = "1.0.0",
                    Title = "Weather Proxy API",
                    Description = "A simple proxy API for weather data from Open-Meteo",
                    Contact = new OpenApiContact
                    {
                        Name = "Weather Proxy API",
                        Email = "support@weatherproxy.api"
                    }
                },
                Servers = new List<OpenApiServer>
                {
                    new() { Url = "http://localhost:7071", Description = "Local Development" }
                }
            });
        });

        return builder;
    }
}
