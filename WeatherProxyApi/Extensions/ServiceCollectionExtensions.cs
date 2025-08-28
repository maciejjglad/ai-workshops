using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.DependencyInjection;
using FluentValidation;
using WeatherProxyApi.Services;
using WeatherProxyApi.Validation;
using WeatherProxyApi.Models;
using System.Net;

namespace WeatherProxyApi.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWeatherApiServices(this IServiceCollection services)
    {
        // Register core services
        services.AddScoped<IWeatherApiClient, WeatherApiClient>();
        services.AddScoped<IWeatherService, WeatherService>();

        // Register validators
        services.AddValidatorsFromAssemblyContaining<CitySearchRequestValidator>();

        // Configure JSON serialization
        services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.TypeInfoResolverChain.Insert(0, WeatherApiJsonContext.Default);
        });

        return services;
    }

    public static IServiceCollection AddWeatherApiHttpClients(this IServiceCollection services)
    {
        // Geocoding API client with resilience
        services.AddHttpClient("OpenMeteoGeocoding", client =>
        {
            client.BaseAddress = new Uri("https://geocoding-api.open-meteo.com/");
            client.DefaultRequestHeaders.Add("User-Agent", "WeatherProxy/1.0");
            client.Timeout = TimeSpan.FromSeconds(6); // Overall timeout
        })
        .AddStandardResilienceHandler(options =>
        {
            // Timeout Policy
            options.TotalRequestTimeout = new HttpTimeoutStrategyOptions
            {
                Timeout = TimeSpan.FromSeconds(4)
            };

            // Retry Policy  
            options.Retry = new HttpRetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromMilliseconds(500),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .Handle<HttpRequestException>()
                    .Handle<TaskCanceledException>()
                    .HandleResult(response => 
                        response.StatusCode >= HttpStatusCode.InternalServerError ||
                        response.StatusCode == HttpStatusCode.RequestTimeout ||
                        response.StatusCode == HttpStatusCode.TooManyRequests)
            };

            // Circuit Breaker
            options.CircuitBreaker = new HttpCircuitBreakerStrategyOptions
            {
                FailureRatio = 0.5,
                SamplingDuration = TimeSpan.FromSeconds(30),
                MinimumThroughput = 5,
                BreakDuration = TimeSpan.FromSeconds(15)
            };
        });

        // Forecast API client with resilience (longer timeout for weather data)
        services.AddHttpClient("OpenMeteoForecast", client =>
        {
            client.BaseAddress = new Uri("https://api.open-meteo.com/");
            client.DefaultRequestHeaders.Add("User-Agent", "WeatherProxy/1.0");
            client.Timeout = TimeSpan.FromSeconds(8); // Longer for weather data
        })
        .AddStandardResilienceHandler(options =>
        {
            // Timeout Policy (longer for weather data)
            options.TotalRequestTimeout = new HttpTimeoutStrategyOptions
            {
                Timeout = TimeSpan.FromSeconds(6)
            };

            // Retry Policy (same as geocoding)
            options.Retry = new HttpRetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromMilliseconds(500),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .Handle<HttpRequestException>()
                    .Handle<TaskCanceledException>()
                    .HandleResult(response => 
                        response.StatusCode >= HttpStatusCode.InternalServerError ||
                        response.StatusCode == HttpStatusCode.RequestTimeout ||
                        response.StatusCode == HttpStatusCode.TooManyRequests)
            };

            // Circuit Breaker (same as geocoding)
            options.CircuitBreaker = new HttpCircuitBreakerStrategyOptions
            {
                FailureRatio = 0.5,
                SamplingDuration = TimeSpan.FromSeconds(30),
                MinimumThroughput = 5,
                BreakDuration = TimeSpan.FromSeconds(15)
            };
        });

        return services;
    }

    public static IServiceCollection AddWeatherApiCors(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("LocalDevelopment", policy =>
            {
                policy.WithOrigins("http://localhost:4200")
                      .WithMethods("GET", "OPTIONS")
                      .WithHeaders("Content-Type", "x-correlation-id")
                      .AllowCredentials()
                      .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
            });
        });

        return services;
    }
}