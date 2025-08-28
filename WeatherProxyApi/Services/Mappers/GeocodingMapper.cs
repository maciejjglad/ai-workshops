using WeatherProxyApi.Models.External;
using WeatherProxyApi.Models.Responses;
using WeatherProxyApi.Utils;

namespace WeatherProxyApi.Services.Mappers;

/// <summary>
/// Mapping helpers for geocoding data transformation
/// </summary>
public static class GeocodingMapper
{
    /// <summary>
    /// Maps external geocoding result to public DTO
    /// </summary>
    /// <param name="external">External API city result</param>
    /// <returns>Public API city result</returns>
    public static CityResult MapToPublicDto(OpenMeteoCityResult external)
    {
        ArgumentNullException.ThrowIfNull(external);

        return new CityResult
        {
            Name = external.Name,
            Country = ResolveCountryName(external),
            Latitude = RoundCoordinate(external.Latitude),
            Longitude = RoundCoordinate(external.Longitude),
            Region = external.Admin1,
            Population = external.Population
        };
    }

    /// <summary>
    /// Maps collection of external geocoding results to public DTOs
    /// </summary>
    /// <param name="externalResults">Collection of external API results</param>
    /// <returns>Collection of public API results</returns>
    public static List<CityResult> MapToPublicDtos(IEnumerable<OpenMeteoCityResult>? externalResults)
    {
        if (externalResults == null)
            return [];

        return externalResults
            .Where(r => !string.IsNullOrWhiteSpace(r.Name))
            .Select(MapToPublicDto)
            .ToList();
    }

    /// <summary>
    /// Resolves country name from external data with fallback logic
    /// </summary>
    /// <param name="external">External city result</param>
    /// <returns>Country name</returns>
    private static string ResolveCountryName(OpenMeteoCityResult external)
    {
        // Prefer full country name if available
        if (!string.IsNullOrWhiteSpace(external.Country))
            return external.Country;

        // Fallback to country code mapping
        if (!string.IsNullOrWhiteSpace(external.CountryCode))
            return CountryCodeMapper.GetCountryName(external.CountryCode);

        // Final fallback
        return "Unknown";
    }

    /// <summary>
    /// Rounds coordinate to 6 decimal places for consistent precision
    /// </summary>
    /// <param name="coordinate">Raw coordinate value</param>
    /// <returns>Rounded coordinate</returns>
    private static double RoundCoordinate(double coordinate)
    {
        return Math.Round(coordinate, 6);
    }
}
