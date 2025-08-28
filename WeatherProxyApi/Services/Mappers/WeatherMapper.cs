using WeatherProxyApi.Models.External;
using WeatherProxyApi.Models.Responses;
using WeatherProxyApi.Utils;

namespace WeatherProxyApi.Services.Mappers;

/// <summary>
/// Mapping helpers for weather data transformation
/// </summary>
public static class WeatherMapper
{
    /// <summary>
    /// Maps external weather response to public DTO
    /// </summary>
    /// <param name="external">External API weather response</param>
    /// <param name="cityName">Optional city name for location data</param>
    /// <param name="countryName">Optional country name for location data</param>
    /// <returns>Public API weather response</returns>
    public static WeatherResponse MapToPublicDto(
        OpenMeteoWeatherResponse external,
        string? cityName = null,
        string? countryName = null)
    {
        ArgumentNullException.ThrowIfNull(external);

        return new WeatherResponse
        {
            Location = MapLocationData(external, cityName, countryName),
            Current = MapCurrentWeatherData(external.Current),
            Daily = MapDailyWeatherData(external.Daily),
            Source = new SourceInfo
            {
                Provider = "open-meteo",
                Model = "best_match"
            }
        };
    }

    /// <summary>
    /// Maps location metadata from external response
    /// </summary>
    /// <param name="external">External weather response</param>
    /// <param name="cityName">Optional city name</param>
    /// <param name="countryName">Optional country name</param>
    /// <returns>Location data DTO</returns>
    private static LocationData MapLocationData(
        OpenMeteoWeatherResponse external,
        string? cityName,
        string? countryName)
    {
        return new LocationData
        {
            Name = cityName ?? "Unknown Location",
            Country = countryName ?? "Unknown",
            Latitude = RoundCoordinate(external.Latitude),
            Longitude = RoundCoordinate(external.Longitude),
            Timezone = external.Timezone
        };
    }

    /// <summary>
    /// Maps current weather data from external response
    /// </summary>
    /// <param name="external">External current weather data</param>
    /// <returns>Current weather DTO</returns>
    private static CurrentWeatherData MapCurrentWeatherData(CurrentWeather? external)
    {
        if (external == null)
        {
            // Return default values if no current weather data
            return new CurrentWeatherData
            {
                Time = DateTime.UtcNow,
                TemperatureC = 0,
                WindSpeedKph = 0,
                WeatherCode = 0,
                IsDay = true,
                Condition = "Unknown",
                Icon = "01d"
            };
        }

        var isDay = external.IsDay == 1;
        var (condition, icon) = WeatherCodeMapper.GetWeatherInfo(external.WeatherCode, isDay);

        return new CurrentWeatherData
        {
            Time = external.Time,
            TemperatureC = RoundTemperature(external.Temperature),
            WindSpeedKph = UnitConverter.ConvertWindSpeedToKmh(external.WindSpeed),
            WeatherCode = external.WeatherCode,
            IsDay = isDay,
            Condition = condition,
            Icon = icon
        };
    }

    /// <summary>
    /// Maps daily weather data from external response
    /// </summary>
    /// <param name="external">External daily weather data</param>
    /// <returns>Collection of daily weather DTOs</returns>
    private static List<DailyWeatherData> MapDailyWeatherData(DailyWeather? external)
    {
        if (external?.Time == null || external.Time.Count == 0)
            return [];

        var result = new List<DailyWeatherData>();
        var dayCount = external.Time.Count;

        for (int i = 0; i < dayCount; i++)
        {
            var weatherCode = GetArrayValueOrDefault(external.WeatherCode, i, 0);
            var (condition, icon) = WeatherCodeMapper.GetWeatherInfo(weatherCode, true);

            result.Add(new DailyWeatherData
            {
                Date = external.Time[i].ToString("yyyy-MM-dd"),
                TemperatureMaxC = RoundTemperature(GetArrayValueOrDefault(external.TemperatureMax, i, 0)),
                TemperatureMinC = RoundTemperature(GetArrayValueOrDefault(external.TemperatureMin, i, 0)),
                PrecipitationProbabilityPct = GetArrayValueOrDefault(external.PrecipitationProbabilityMax, i, 0),
                WindSpeedMaxKph = UnitConverter.ConvertWindSpeedToKmh(
                    GetArrayValueOrDefault(external.WindSpeedMax, i, 0)),
                WeatherCode = weatherCode,
                Condition = condition,
                Icon = icon
            });
        }

        return result;
    }

    /// <summary>
    /// Safely gets array value at index with fallback
    /// </summary>
    /// <typeparam name="T">Array element type</typeparam>
    /// <param name="array">Source array</param>
    /// <param name="index">Target index</param>
    /// <param name="defaultValue">Fallback value</param>
    /// <returns>Array value or default</returns>
    private static T GetArrayValueOrDefault<T>(List<T> array, int index, T defaultValue)
    {
        return array != null && index >= 0 && index < array.Count 
            ? array[index] 
            : defaultValue;
    }

    /// <summary>
    /// Rounds temperature to 1 decimal place
    /// </summary>
    /// <param name="temperature">Raw temperature value</param>
    /// <returns>Rounded temperature</returns>
    private static double RoundTemperature(double temperature)
    {
        return Math.Round(temperature, 1);
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
