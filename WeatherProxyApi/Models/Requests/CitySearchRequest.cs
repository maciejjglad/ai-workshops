namespace WeatherProxyApi.Models.Requests;

public record CitySearchRequest
{
    public required string Q { get; init; }
    public int Count { get; init; } = 5;
    public string Language { get; init; } = "en";
}
