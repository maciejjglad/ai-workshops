# Weather API DTOs & Source Generation Context

## External JSON Fragments (Open-Meteo)

### Geocoding API Response
```json
{
  "results": [
    {
      "id": 2950159,
      "name": "Berlin",
      "latitude": 52.52437,
      "longitude": 13.41053,
      "elevation": 74.0,
      "feature_code": "PPLC",
      "country_code": "DE",
      "admin1": "Berlin",
      "country": "Germany",
      "timezone": "Europe/Berlin",
      "population": 3426354
    }
  ]
}
```

### Weather Forecast API Response
```json
{
  "latitude": 52.52,
  "longitude": 13.41,
  "timezone": "Europe/Berlin",
  "timezone_abbreviation": "CET",
  "elevation": 34.0,
  "current_units": {
    "time": "iso8601",
    "temperature_2m": "°C",
    "wind_speed_10m": "m/s",
    "weather_code": "wmo code"
  },
  "current": {
    "time": "2024-01-15T14:00",
    "temperature_2m": 15.2,
    "wind_speed_10m": 3.5,
    "is_day": 1,
    "weather_code": 3
  },
  "daily_units": {
    "time": "iso8601",
    "temperature_2m_max": "°C",
    "temperature_2m_min": "°C",
    "precipitation_probability_max": "%",
    "wind_speed_10m_max": "m/s",
    "weather_code": "wmo code"
  },
  "daily": {
    "time": ["2024-01-15", "2024-01-16"],
    "weather_code": [3, 61],
    "temperature_2m_max": [15.2, 18.1],
    "temperature_2m_min": [8.3, 12.5],
    "precipitation_probability_max": [20, 80],
    "wind_speed_10m_max": [12.5, 15.8]
  }
}
```

## DTOs for `/api/cities/search`

### External API DTOs (Open-Meteo)
```csharp
// External API response mapping
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
```

### Public API DTOs
```csharp
// Request DTO
public record CitySearchRequest
{
    public required string Q { get; init; }
    public int Count { get; init; } = 5;
    public string Language { get; init; } = "en";
}

// Response DTO
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
    public string? Region { get; init; }      // Admin1
    public int? Population { get; init; }
}
```

## DTOs for `/api/weather`

### External API DTOs (Open-Meteo)
```csharp
// External API response mapping
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
```

### Public API DTOs
```csharp
// Request DTO
public record WeatherRequest
{
    public double Lat { get; init; }
    public double Lon { get; init; }
    public int Days { get; init; } = 5;
}

// Response DTO
public record WeatherResponse
{
    public required CurrentWeatherData Current { get; init; }
    public required List<DailyWeatherData> Daily { get; init; }
    public required string Timezone { get; init; }
    public required string LocalTime { get; init; }
}

public record CurrentWeatherData
{
    public double Temperature { get; init; }           // °C
    public double WindSpeed { get; init; }             // km/h (converted from m/s)
    public required string Condition { get; init; }    // Weather description
    public required string Icon { get; init; }         // Weather icon code
    public bool IsDay { get; init; }
    public int WeatherCode { get; init; }
}

public record DailyWeatherData
{
    public required string Date { get; init; }         // ISO date string
    public double TemperatureMax { get; init; }        // °C
    public double TemperatureMin { get; init; }        // °C
    public int PrecipitationProbability { get; init; } // %
    public double WindSpeedMax { get; init; }          // km/h (converted from m/s)
    public required string Condition { get; init; }    // Weather description
    public required string Icon { get; init; }         // Weather icon code
    public int WeatherCode { get; init; }
}
```

## JsonSerializerContext with Source Generation

```csharp
using System.Text.Json.Serialization;

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
[JsonSerializable(typeof(CurrentWeatherData))]
[JsonSerializable(typeof(DailyWeatherData))]
[JsonSerializable(typeof(List<CityResult>))]
[JsonSerializable(typeof(List<DailyWeatherData>))]
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
```

## Units and Conversions

### Wind Speed Conversion
```csharp
// Open-Meteo returns wind speed in m/s, convert to km/h for UI
public static double ConvertWindSpeedToKmh(double windSpeedMps)
    => Math.Round(windSpeedMps * 3.6, 1);
```

### Temperature Conversion (if needed)
```csharp
// Open-Meteo returns °C, convert to °F if UI requires
public static double ConvertToFahrenheit(double celsius)
    => Math.Round((celsius * 9.0 / 5.0) + 32, 1);
```

### Weather Code Mapping
```csharp
// WMO Weather interpretation codes
public static (string Condition, string Icon) GetWeatherInfo(int code, bool isDay)
{
    return code switch
    {
        0 => ("Clear sky", isDay ? "01d" : "01n"),
        1 => ("Mainly clear", isDay ? "02d" : "02n"),
        2 => ("Partly cloudy", isDay ? "03d" : "03n"),
        3 => ("Overcast", "04d"),
        45 or 48 => ("Fog", "50d"),
        51 or 53 or 55 => ("Drizzle", "09d"),
        61 or 63 or 65 => ("Rain", "10d"),
        71 or 73 or 75 => ("Snow", "13d"),
        95 => ("Thunderstorm", "11d"),
        96 or 99 => ("Thunderstorm with hail", "11d"),
        _ => ("Unknown", "01d")
    };
}
```

### Date/Time Handling
```csharp
// Convert Open-Meteo timezone to local time display
public static string FormatLocalTime(DateTime utcTime, string timezone)
{
    var timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(timezone);
    var localTime = TimeZoneInfo.ConvertTimeFromUtc(utcTime, timeZoneInfo);
    return localTime.ToString("HH:mm");
}
```

## Validation Rules

### CitySearchRequest Validation
```csharp
public class CitySearchRequestValidator : AbstractValidator<CitySearchRequest>
{
    public CitySearchRequestValidator()
    {
        RuleFor(x => x.Q)
            .NotEmpty()
            .MinimumLength(2)
            .MaximumLength(100)
            .WithMessage("Search query must be between 2 and 100 characters");
            
        RuleFor(x => x.Count)
            .GreaterThan(0)
            .LessThanOrEqualTo(10)
            .WithMessage("Count must be between 1 and 10");
    }
}
```

### WeatherRequest Validation
```csharp
public class WeatherRequestValidator : AbstractValidator<WeatherRequest>
{
    public WeatherRequestValidator()
    {
        RuleFor(x => x.Lat)
            .GreaterThanOrEqualTo(-90)
            .LessThanOrEqualTo(90)
            .WithMessage("Latitude must be between -90 and 90");
            
        RuleFor(x => x.Lon)
            .GreaterThanOrEqualTo(-180)
            .LessThanOrEqualTo(180)
            .WithMessage("Longitude must be between -180 and 180");
            
        RuleFor(x => x.Days)
            .GreaterThan(0)
            .LessThanOrEqualTo(7)
            .WithMessage("Days must be between 1 and 7");
    }
}
```
