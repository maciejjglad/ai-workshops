using WeatherProxyApi.Models.External;
using WeatherProxyApi.Models.Requests;
using WeatherProxyApi.Models.Responses;

namespace WeatherProxyApi.Tests.TestFixtures;

/// <summary>
/// Provides test data for unit and integration tests
/// </summary>
public static class TestData
{
    #region City Search Test Data

    public static CitySearchRequest ValidCitySearchRequest => new()
    {
        Q = "London",
        Count = 5,
        Language = "en"
    };

    public static CitySearchRequest KrakowSearchRequest => new()
    {
        Q = "Kraków",
        Count = 3,
        Language = "pl"
    };

    public static CitySearchRequest EmptyQueryRequest => new()
    {
        Q = "",
        Count = 5,
        Language = "en"
    };

    public static CitySearchRequest InvalidCountRequest => new()
    {
        Q = "Berlin",
        Count = 0,
        Language = "en"
    };

    public static OpenMeteoCityResult LondonCityResult => new()
    {
        Id = 2643743,
        Name = "London",
        Latitude = 51.50853,
        Longitude = -0.12574,
        CountryCode = "GB",
        Country = "United Kingdom",
        Admin1 = "England",
        Timezone = "Europe/London",
        Population = 8982000
    };

    public static OpenMeteoCityResult KrakowCityResult => new()
    {
        Id = 3094802,
        Name = "Kraków",
        Latitude = 50.06143,
        Longitude = 19.93658,
        CountryCode = "PL",
        Country = "Poland",
        Admin1 = "Lesser Poland Voivodeship",
        Timezone = "Europe/Warsaw",
        Population = 779115
    };

    public static OpenMeteoGeocodingResponse ValidGeocodingResponse => new()
    {
        Results = [LondonCityResult, KrakowCityResult]
    };

    public static OpenMeteoGeocodingResponse EmptyGeocodingResponse => new()
    {
        Results = []
    };

    #endregion

    #region Weather Test Data

    public static WeatherRequest ValidWeatherRequest => new()
    {
        Lat = 51.5074,
        Lon = -0.1278,
        Days = 5
    };

    public static WeatherRequest InvalidLatitudeRequest => new()
    {
        Lat = 91.0,  // Invalid latitude
        Lon = 0.0,
        Days = 5
    };

    public static WeatherRequest InvalidLongitudeRequest => new()
    {
        Lat = 0.0,
        Lon = 181.0,  // Invalid longitude
        Days = 5
    };

    public static CurrentWeather ValidCurrentWeather => new()
    {
        Time = DateTime.Parse("2024-01-15T14:30:00"),
        Temperature = 15.2,
        WindSpeed = 3.5,
        IsDay = 1,
        WeatherCode = 3
    };

    public static DailyWeather ValidDailyWeather => new()
    {
        Time = [
            DateOnly.Parse("2024-01-15"),
            DateOnly.Parse("2024-01-16"),
            DateOnly.Parse("2024-01-17")
        ],
        WeatherCode = [3, 61, 0],
        TemperatureMax = [18.1, 22.3, 25.0],
        TemperatureMin = [8.3, 12.1, 15.2],
        PrecipitationProbabilityMax = [20, 80, 5],
        WindSpeedMax = [12.5, 15.8, 8.2]
    };

    public static OpenMeteoWeatherResponse ValidWeatherResponse => new()
    {
        Latitude = 51.5074,
        Longitude = -0.1278,
        Timezone = "Europe/London",
        TimezoneAbbreviation = "GMT",
        Elevation = 23.0,
        Current = ValidCurrentWeather,
        Daily = ValidDailyWeather
    };

    #endregion

    #region Error Test Data

    public static readonly string MalformedJsonResponse = "{ invalid json }";
    
    public static readonly string ValidJsonWithMissingFields = @"{
        ""results"": [{
            ""name"": ""Test City""
            // Missing required fields like latitude, longitude, country_code
        }]
    }";

    public static readonly string EmptyJsonResponse = "{}";

    #endregion

    #region Expected Mapping Results

    public static CityResult ExpectedLondonResult => new()
    {
        Name = "London",
        Country = "United Kingdom",
        Latitude = 51.508530,  // Rounded to 6 decimals
        Longitude = -0.125740,  // Rounded to 6 decimals
        Region = "England",
        Population = 8982000
    };

    public static CurrentWeatherData ExpectedCurrentWeather => new()
    {
        Time = DateTime.Parse("2024-01-15T14:30:00"),
        TemperatureC = 15.2,
        WindSpeedKph = 12.6,  // 3.5 m/s * 3.6
        WeatherCode = 3,
        IsDay = true,
        Condition = "Overcast",
        Icon = "04d"
    };

    public static DailyWeatherData ExpectedFirstDayWeather => new()
    {
        Date = "2024-01-15",
        TemperatureMaxC = 18.1,
        TemperatureMinC = 8.3,
        PrecipitationProbabilityPct = 20,
        WindSpeedMaxKph = 45.0,  // 12.5 m/s * 3.6
        WeatherCode = 3,
        Condition = "Overcast",
        Icon = "04d"
    };

    #endregion
}
