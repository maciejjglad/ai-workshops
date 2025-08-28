using Microsoft.Extensions.Logging;
using WeatherProxyApi.Models.Requests;
using WeatherProxyApi.Models.Responses;
using WeatherProxyApi.Services.Exceptions;
using WeatherProxyApi.Services.Mappers;

namespace WeatherProxyApi.Services;

/// <summary>
/// High-level weather service providing business logic and data transformation
/// </summary>
public class WeatherService : IWeatherService
{
    private readonly IWeatherApiClient _apiClient;
    private readonly ILogger<WeatherService> _logger;

    public WeatherService(
        IWeatherApiClient apiClient,
        ILogger<WeatherService> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    public async Task<List<CityResult>> SearchCitiesAsync(
        CitySearchRequest request, 
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        _logger.LogInformation("Searching cities for query: '{Query}', count: {Count}, language: {Language}",
            request.Q, request.Count, request.Language);

        try
        {
            var response = await _apiClient.GetGeocodingAsync(
                request.Q, 
                request.Count, 
                request.Language, 
                cancellationToken);

            var cities = GeocodingMapper.MapToPublicDtos(response?.Results);

            _logger.LogInformation("City search completed. Found {CityCount} cities for query: '{Query}'",
                cities.Count, request.Q);

            return cities;
        }
        catch (ExternalApiException ex) when (IsNotFoundError(ex))
        {
            _logger.LogInformation("No cities found for query: '{Query}'", request.Q);
            return [];
        }
        catch (ExternalApiException ex)
        {
            _logger.LogError(ex, "External API error during city search for query: '{Query}'", request.Q);
            throw new InvalidOperationException(
                "Failed to retrieve city data from external service", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during city search for query: '{Query}'", request.Q);
            throw new InvalidOperationException(
                "An unexpected error occurred while searching for cities", ex);
        }
    }

    public async Task<WeatherResponse> GetWeatherAsync(
        WeatherRequest request, 
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        _logger.LogInformation("Getting weather for coordinates: ({Latitude}, {Longitude}), days: {Days}",
            request.Lat, request.Lon, request.Days);

        try
        {
            var response = await _apiClient.GetForecastAsync(
                request.Lat, 
                request.Lon, 
                request.Days, 
                cancellationToken);

            if (response == null)
            {
                throw new InvalidOperationException("Received null response from weather service");
            }

            // Attempt to get location name from reverse geocoding (optional enhancement)
            var (cityName, countryName) = await TryGetLocationNameAsync(
                request.Lat, request.Lon, cancellationToken);

            var weather = WeatherMapper.MapToPublicDto(response, cityName, countryName);

            _logger.LogInformation("Weather forecast completed for coordinates: ({Latitude}, {Longitude})",
                request.Lat, request.Lon);

            return weather;
        }
        catch (ExternalApiException ex)
        {
            _logger.LogError(ex, "External API error during weather forecast for coordinates: ({Latitude}, {Longitude})",
                request.Lat, request.Lon);
            throw new InvalidOperationException(
                "Failed to retrieve weather data from external service", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during weather forecast for coordinates: ({Latitude}, {Longitude})",
                request.Lat, request.Lon);
            throw new InvalidOperationException(
                "An unexpected error occurred while retrieving weather data", ex);
        }
    }

    /// <summary>
    /// Attempts to get location name through reverse geocoding
    /// This is optional and failures are handled gracefully
    /// </summary>
    /// <param name="latitude">Latitude coordinate</param>
    /// <param name="longitude">Longitude coordinate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Tuple of city name and country name, or nulls if not found</returns>
    private async Task<(string? CityName, string? CountryName)> TryGetLocationNameAsync(
        double latitude, 
        double longitude, 
        CancellationToken cancellationToken)
    {
        try
        {
            // Simple reverse geocoding attempt using a broad search
            // This is best-effort and may not always return results
            var searchQuery = $"{latitude:F2},{longitude:F2}";
            
            var response = await _apiClient.GetGeocodingAsync(
                searchQuery, 1, "en", cancellationToken);

            var firstResult = response?.Results?.FirstOrDefault();
            if (firstResult != null)
            {
                var cityResult = GeocodingMapper.MapToPublicDto(firstResult);
                return (cityResult.Name, cityResult.Country);
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to reverse geocode coordinates: ({Latitude}, {Longitude}). " +
                "This is not critical and weather data will still be returned.", latitude, longitude);
        }

        return (null, null);
    }

    /// <summary>
    /// Checks if an external API exception represents a "not found" scenario
    /// </summary>
    /// <param name="ex">External API exception</param>
    /// <returns>True if this represents a not found error</returns>
    private static bool IsNotFoundError(ExternalApiException ex)
    {
        return ex.HttpStatusCode == 404 || 
               (ex.ResponseContent?.Contains("no results", StringComparison.OrdinalIgnoreCase) == true);
    }
}