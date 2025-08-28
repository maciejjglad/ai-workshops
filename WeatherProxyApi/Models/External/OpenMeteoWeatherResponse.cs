using System.Text.Json.Serialization;

namespace WeatherProxyApi.Models.External;

public record OpenMeteoWeatherResponse
{
    [JsonPropertyName("latitude")]
    public double Latitude { get; init; }
    
    [JsonPropertyName("longitude")]
    public double Longitude { get; init; }
    
    [JsonPropertyName("timezone")]
    public required string Timezone { get; init; }
    
    [JsonPropertyName("timezone_abbreviation")]
    public required string TimezoneAbbreviation { get; init; }
    
    [JsonPropertyName("elevation")]
    public double Elevation { get; init; }
    
    [JsonPropertyName("current_units")]
    public WeatherUnits? CurrentUnits { get; init; }
    
    [JsonPropertyName("current")]
    public CurrentWeather? Current { get; init; }
    
    [JsonPropertyName("daily_units")]
    public DailyWeatherUnits? DailyUnits { get; init; }
    
    [JsonPropertyName("daily")]
    public DailyWeather? Daily { get; init; }
}

public record WeatherUnits
{
    [JsonPropertyName("time")]
    public string? Time { get; init; }
    
    [JsonPropertyName("temperature_2m")]
    public string? Temperature { get; init; }
    
    [JsonPropertyName("wind_speed_10m")]
    public string? WindSpeed { get; init; }
    
    [JsonPropertyName("weather_code")]
    public string? WeatherCode { get; init; }
}

public record CurrentWeather
{
    [JsonPropertyName("time")]
    public DateTime Time { get; init; }
    
    [JsonPropertyName("temperature_2m")]
    public double Temperature { get; init; }
    
    [JsonPropertyName("wind_speed_10m")]
    public double WindSpeed { get; init; }
    
    [JsonPropertyName("is_day")]
    public int IsDay { get; init; }
    
    [JsonPropertyName("weather_code")]
    public int WeatherCode { get; init; }
}

public record DailyWeatherUnits
{
    [JsonPropertyName("time")]
    public string? Time { get; init; }
    
    [JsonPropertyName("temperature_2m_max")]
    public string? TemperatureMax { get; init; }
    
    [JsonPropertyName("temperature_2m_min")]
    public string? TemperatureMin { get; init; }
    
    [JsonPropertyName("precipitation_probability_max")]
    public string? PrecipitationProbability { get; init; }
    
    [JsonPropertyName("wind_speed_10m_max")]
    public string? WindSpeedMax { get; init; }
    
    [JsonPropertyName("weather_code")]
    public string? WeatherCode { get; init; }
}

public record DailyWeather
{
    [JsonPropertyName("time")]
    public List<DateOnly> Time { get; init; } = [];
    
    [JsonPropertyName("weather_code")]
    public List<int> WeatherCode { get; init; } = [];
    
    [JsonPropertyName("temperature_2m_max")]
    public List<double> TemperatureMax { get; init; } = [];
    
    [JsonPropertyName("temperature_2m_min")]
    public List<double> TemperatureMin { get; init; } = [];
    
    [JsonPropertyName("precipitation_probability_max")]
    public List<int> PrecipitationProbabilityMax { get; init; } = [];
    
    [JsonPropertyName("wind_speed_10m_max")]
    public List<double> WindSpeedMax { get; init; } = [];
}
