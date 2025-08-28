namespace WeatherProxyApi.Models.Responses;

public record WeatherResponse
{
    public required LocationData Location { get; init; }
    public required CurrentWeatherData Current { get; init; }
    public required List<DailyWeatherData> Daily { get; init; }
    public required SourceInfo Source { get; init; }
}

public record LocationData
{
    public required string Name { get; init; }
    public required string Country { get; init; }
    public double Latitude { get; init; }
    public double Longitude { get; init; }
    public required string Timezone { get; init; }
}

public record CurrentWeatherData
{
    public DateTime Time { get; init; }
    public double TemperatureC { get; init; }
    public double WindSpeedKph { get; init; }
    public int WeatherCode { get; init; }
    public bool IsDay { get; init; }
    public required string Condition { get; init; }
    public required string Icon { get; init; }
}

public record DailyWeatherData
{
    public required string Date { get; init; }
    public double TemperatureMaxC { get; init; }
    public double TemperatureMinC { get; init; }
    public int PrecipitationProbabilityPct { get; init; }
    public double WindSpeedMaxKph { get; init; }
    public int WeatherCode { get; init; }
    public required string Condition { get; init; }
    public required string Icon { get; init; }
}

public record SourceInfo
{
    public required string Provider { get; init; }
    public string? Model { get; init; }
}
