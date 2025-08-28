using System.Text.Json;
using Microsoft.Extensions.Logging;
using WeatherProxyApi.Models;
using WeatherProxyApi.Models.External;
using WeatherProxyApi.Services.Exceptions;

namespace WeatherProxyApi.Services;

/// <summary>
/// HTTP client wrapper for Open-Meteo API with resilience and error handling
/// </summary>
public class WeatherApiClient : IWeatherApiClient
{
    private readonly HttpClient _geocodingClient;
    private readonly HttpClient _forecastClient;
    private readonly ILogger<WeatherApiClient> _logger;

    public WeatherApiClient(
        IHttpClientFactory httpClientFactory,
        ILogger<WeatherApiClient> logger)
    {
        _geocodingClient = httpClientFactory.CreateClient("OpenMeteoGeocoding");
        _forecastClient = httpClientFactory.CreateClient("OpenMeteoForecast");
        _logger = logger;
    }

    public async Task<OpenMeteoGeocodingResponse?> GetGeocodingAsync(
        string name, 
        int count, 
        string language, 
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);

        var url = BuildGeocodingUrl(name, count, language);
        
        _logger.LogInformation("Calling Open-Meteo Geocoding API: {Url}", url);

        try
        {
            var response = await _geocodingClient.GetAsync(url, cancellationToken);
            
            await HandleHttpErrorsAsync(response, "geocoding");
            
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            
            if (string.IsNullOrWhiteSpace(content))
            {
                _logger.LogWarning("Empty response from geocoding API for query: {Name}", name);
                return new OpenMeteoGeocodingResponse { Results = [] };
            }

            return DeserializeGeocodingResponse(content);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error calling geocoding API for query: {Name}", name);
            throw new ExternalApiException("Failed to retrieve geocoding data from Open-Meteo API", ex);
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogError(ex, "Timeout calling geocoding API for query: {Name}", name);
            throw new ExternalApiException("Geocoding API request timed out", ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON parsing error from geocoding API for query: {Name}", name);
            throw new ExternalApiException("Invalid response format from geocoding API", ex);
        }
    }

    public async Task<OpenMeteoWeatherResponse?> GetForecastAsync(
        double latitude, 
        double longitude, 
        int forecastDays, 
        CancellationToken cancellationToken = default)
    {
        ValidateCoordinates(latitude, longitude);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(forecastDays);

        var url = BuildForecastUrl(latitude, longitude, forecastDays);
        
        _logger.LogInformation("Calling Open-Meteo Forecast API: {Url}", url);

        try
        {
            var response = await _forecastClient.GetAsync(url, cancellationToken);
            
            await HandleHttpErrorsAsync(response, "forecast");
            
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            
            if (string.IsNullOrWhiteSpace(content))
            {
                _logger.LogError("Empty response from forecast API for coordinates: ({Latitude}, {Longitude})", 
                    latitude, longitude);
                throw new ExternalApiException("Empty response from weather API");
            }

            return DeserializeForecastResponse(content);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error calling forecast API for coordinates: ({Latitude}, {Longitude})", 
                latitude, longitude);
            throw new ExternalApiException("Failed to retrieve weather data from Open-Meteo API", ex);
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            _logger.LogError(ex, "Timeout calling forecast API for coordinates: ({Latitude}, {Longitude})", 
                latitude, longitude);
            throw new ExternalApiException("Weather API request timed out", ex);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON parsing error from forecast API for coordinates: ({Latitude}, {Longitude})", 
                latitude, longitude);
            throw new ExternalApiException("Invalid response format from weather API", ex);
        }
    }

    private static string BuildGeocodingUrl(string name, int count, string language)
    {
        return $"v1/search?name={Uri.EscapeDataString(name)}" +
               $"&count={count}" +
               $"&language={language}" +
               "&format=json";
    }

    private static string BuildForecastUrl(double latitude, double longitude, int forecastDays)
    {
        return $"v1/forecast?latitude={latitude:F6}&longitude={longitude:F6}" +
               "&current=temperature_2m,wind_speed_10m,is_day,weather_code" +
               "&daily=weather_code,temperature_2m_max,temperature_2m_min,precipitation_probability_max,wind_speed_10m_max" +
               "&timezone=auto" +
               $"&forecast_days={forecastDays}";
    }

    private static void ValidateCoordinates(double latitude, double longitude)
    {
        if (latitude < -90 || latitude > 90)
        {
            throw new ArgumentOutOfRangeException(nameof(latitude), latitude, 
                "Latitude must be between -90 and 90 degrees");
        }

        if (longitude < -180 || longitude > 180)
        {
            throw new ArgumentOutOfRangeException(nameof(longitude), longitude, 
                "Longitude must be between -180 and 180 degrees");
        }
    }

    private async Task HandleHttpErrorsAsync(HttpResponseMessage response, string apiType)
    {
        if (response.IsSuccessStatusCode)
            return;

        var content = await response.Content.ReadAsStringAsync();
        var statusCode = (int)response.StatusCode;

        _logger.LogError("HTTP error from {ApiType} API: {StatusCode} - {Content}", 
            apiType, statusCode, content);

        var errorMessage = response.StatusCode switch
        {
            System.Net.HttpStatusCode.BadRequest => "Invalid request parameters",
            System.Net.HttpStatusCode.NotFound => "Resource not found",
            System.Net.HttpStatusCode.UnprocessableEntity => "Invalid coordinates or parameters",
            System.Net.HttpStatusCode.TooManyRequests => "Rate limit exceeded",
            System.Net.HttpStatusCode.InternalServerError => "External service error",
            System.Net.HttpStatusCode.BadGateway => "External service unavailable",
            System.Net.HttpStatusCode.ServiceUnavailable => "External service temporarily unavailable",
            System.Net.HttpStatusCode.GatewayTimeout => "External service timeout",
            _ => "Unknown error from external service"
        };

        throw new ExternalApiException($"{errorMessage} (HTTP {statusCode})", 
            statusCode, content);
    }

    private OpenMeteoGeocodingResponse DeserializeGeocodingResponse(string content)
    {
        try
        {
            return JsonSerializer.Deserialize(content, 
                WeatherApiJsonContext.Default.OpenMeteoGeocodingResponse) 
                ?? new OpenMeteoGeocodingResponse { Results = [] };
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize geocoding response: {Content}", content);
            throw;
        }
    }

    private OpenMeteoWeatherResponse DeserializeForecastResponse(string content)
    {
        try
        {
            var response = JsonSerializer.Deserialize(content, 
                WeatherApiJsonContext.Default.OpenMeteoWeatherResponse);

            if (response == null)
            {
                throw new JsonException("Deserialized weather response is null");
            }

            return response;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize weather response: {Content}", content);
            throw;
        }
    }
}
