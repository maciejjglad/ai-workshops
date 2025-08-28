using WeatherProxyApi.Models.External;

namespace WeatherProxyApi.Services;

/// <summary>
/// Low-level client interface for Open-Meteo API interactions
/// </summary>
public interface IWeatherApiClient
{
    /// <summary>
    /// Get geocoding data for a city search
    /// </summary>
    /// <param name="name">City name to search</param>
    /// <param name="count">Maximum number of results (1-100)</param>
    /// <param name="language">Language code (ISO 639-1)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Raw geocoding response from Open-Meteo API</returns>
    Task<OpenMeteoGeocodingResponse?> GetGeocodingAsync(
        string name, 
        int count, 
        string language, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get weather forecast data for coordinates
    /// </summary>
    /// <param name="latitude">Latitude in decimal degrees (-90 to 90)</param>
    /// <param name="longitude">Longitude in decimal degrees (-180 to 180)</param>
    /// <param name="forecastDays">Number of forecast days (1-16)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Raw weather response from Open-Meteo API</returns>
    Task<OpenMeteoWeatherResponse?> GetForecastAsync(
        double latitude, 
        double longitude, 
        int forecastDays, 
        CancellationToken cancellationToken = default);
}
