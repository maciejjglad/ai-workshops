namespace WeatherProxyApi.Models.Responses;

public record CitySearchResponse
{
    public required List<CityResult> Cities { get; init; }
}

public record CityResult
{
    public required string Name { get; init; }
    public required string Country { get; init; }
    public double Latitude { get; init; }
    public double Longitude { get; init; }
    public string? Region { get; init; }
    public int? Population { get; init; }
}
