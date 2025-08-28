namespace WeatherProxyApi.Models.Requests;

public record WeatherRequest
{
    public double Lat { get; init; }
    public double Lon { get; init; }
    public int Days { get; init; } = 5;
    public string? CityName { get; init; }
    public string? CountryName { get; init; }
}
