using System.Text.Json.Serialization;

namespace WeatherProxyApi.Models.External;

public record OpenMeteoGeocodingResponse
{
    [JsonPropertyName("results")]
    public List<OpenMeteoCityResult>? Results { get; init; }
}

public record OpenMeteoCityResult
{
    [JsonPropertyName("id")]
    public int Id { get; init; }
    
    [JsonPropertyName("name")]
    public required string Name { get; init; }
    
    [JsonPropertyName("latitude")]
    public double Latitude { get; init; }
    
    [JsonPropertyName("longitude")]
    public double Longitude { get; init; }
    
    [JsonPropertyName("elevation")]
    public double? Elevation { get; init; }
    
    [JsonPropertyName("feature_code")]
    public string? FeatureCode { get; init; }
    
    [JsonPropertyName("country_code")]
    public required string CountryCode { get; init; }
    
    [JsonPropertyName("country")]
    public string? Country { get; init; }
    
    [JsonPropertyName("admin1")]
    public string? Admin1 { get; init; }
    
    [JsonPropertyName("timezone")]
    public string? Timezone { get; init; }
    
    [JsonPropertyName("population")]
    public int? Population { get; init; }
}
