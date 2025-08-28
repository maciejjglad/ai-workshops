using System.Text.Json.Serialization;
using WeatherProxyApi.Models.Requests;
using WeatherProxyApi.Models.Responses;
using WeatherProxyApi.Models.External;
using WeatherProxyApi.Models.Errors;

namespace WeatherProxyApi.Models;

[JsonSourceGenerationOptions(
    WriteIndented = false,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    AllowTrailingCommas = true,
    ReadCommentHandling = JsonCommentHandling.Skip)]
[JsonSerializable(typeof(CitySearchRequest))]
[JsonSerializable(typeof(CitySearchResponse))]
[JsonSerializable(typeof(CityResult))]
[JsonSerializable(typeof(WeatherRequest))]
[JsonSerializable(typeof(WeatherResponse))]
[JsonSerializable(typeof(LocationData))]
[JsonSerializable(typeof(CurrentWeatherData))]
[JsonSerializable(typeof(DailyWeatherData))]
[JsonSerializable(typeof(SourceInfo))]
[JsonSerializable(typeof(List<CityResult>))]
[JsonSerializable(typeof(List<DailyWeatherData>))]
[JsonSerializable(typeof(ApiProblemDetails))]
// External API DTOs
[JsonSerializable(typeof(OpenMeteoGeocodingResponse))]
[JsonSerializable(typeof(OpenMeteoCityResult))]
[JsonSerializable(typeof(OpenMeteoWeatherResponse))]
[JsonSerializable(typeof(CurrentWeather))]
[JsonSerializable(typeof(DailyWeather))]
[JsonSerializable(typeof(WeatherUnits))]
[JsonSerializable(typeof(DailyWeatherUnits))]
[JsonSerializable(typeof(List<OpenMeteoCityResult>))]
[JsonSerializable(typeof(List<DateOnly>))]
[JsonSerializable(typeof(List<int>))]
[JsonSerializable(typeof(List<double>))]
public partial class WeatherApiJsonContext : JsonSerializerContext
{
}
