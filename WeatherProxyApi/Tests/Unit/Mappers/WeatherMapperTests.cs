using FluentAssertions;
using WeatherProxyApi.Models.External;
using WeatherProxyApi.Services.Mappers;
using WeatherProxyApi.Tests.TestFixtures;
using Xunit;

namespace WeatherProxyApi.Tests.Unit.Mappers;

/// <summary>
/// Unit tests for WeatherMapper
/// </summary>
public class WeatherMapperTests
{
    [Fact]
    public void MapToPublicDto_ValidResponse_MapsAllFields()
    {
        // Arrange
        var external = TestData.ValidWeatherResponse;

        // Act
        var result = WeatherMapper.MapToPublicDto(external, "London", "United Kingdom");

        // Assert
        result.Should().NotBeNull();
        
        // Location
        result.Location.Name.Should().Be("London");
        result.Location.Country.Should().Be("United Kingdom");
        result.Location.Latitude.Should().Be(51.507400); // Rounded to 6 decimals
        result.Location.Longitude.Should().Be(-0.127800); // Rounded to 6 decimals
        result.Location.Timezone.Should().Be("Europe/London");

        // Current weather
        result.Current.Should().NotBeNull();
        result.Current.Time.Should().Be(DateTime.Parse("2024-01-15T14:30:00"));
        result.Current.TemperatureC.Should().Be(15.2);
        result.Current.WindSpeedKph.Should().Be(12.6); // 3.5 m/s * 3.6
        result.Current.WeatherCode.Should().Be(3);
        result.Current.IsDay.Should().BeTrue();
        result.Current.Condition.Should().Be("Overcast");
        result.Current.Icon.Should().Be("04d");

        // Daily weather
        result.Daily.Should().HaveCount(3);
        
        // Source info
        result.Source.Provider.Should().Be("open-meteo");
        result.Source.Model.Should().Be("best_match");
    }

    [Fact]
    public void MapToPublicDto_WithoutLocationNames_UsesDefaultValues()
    {
        // Arrange
        var external = TestData.ValidWeatherResponse;

        // Act
        var result = WeatherMapper.MapToPublicDto(external);

        // Assert
        result.Location.Name.Should().Be("Unknown Location");
        result.Location.Country.Should().Be("Unknown");
        result.Location.Latitude.Should().Be(51.507400);
        result.Location.Longitude.Should().Be(-0.127800);
    }

    [Fact]
    public void MapToPublicDto_NullCurrentWeather_UsesDefaults()
    {
        // Arrange
        var external = new OpenMeteoWeatherResponse
        {
            Latitude = 51.5074,
            Longitude = -0.1278,
            Timezone = "Europe/London",
            TimezoneAbbreviation = "GMT",
            Elevation = 23.0,
            Current = null, // No current weather data
            Daily = TestData.ValidDailyWeather
        };

        // Act
        var result = WeatherMapper.MapToPublicDto(external);

        // Assert
        result.Current.Should().NotBeNull();
        result.Current.TemperatureC.Should().Be(0);
        result.Current.WindSpeedKph.Should().Be(0);
        result.Current.WeatherCode.Should().Be(0);
        result.Current.IsDay.Should().BeTrue();
        result.Current.Condition.Should().Be("Unknown");
        result.Current.Icon.Should().Be("01d");
    }

    [Fact]
    public void MapToPublicDto_NullDailyWeather_ReturnsEmptyDaily()
    {
        // Arrange
        var external = new OpenMeteoWeatherResponse
        {
            Latitude = 51.5074,
            Longitude = -0.1278,
            Timezone = "Europe/London",
            TimezoneAbbreviation = "GMT",
            Elevation = 23.0,
            Current = TestData.ValidCurrentWeather,
            Daily = null // No daily weather data
        };

        // Act
        var result = WeatherMapper.MapToPublicDto(external);

        // Assert
        result.Daily.Should().NotBeNull();
        result.Daily.Should().BeEmpty();
    }

    [Fact]
    public void MapToPublicDto_EmptyDailyWeather_ReturnsEmptyDaily()
    {
        // Arrange
        var external = new OpenMeteoWeatherResponse
        {
            Latitude = 51.5074,
            Longitude = -0.1278,
            Timezone = "Europe/London",
            TimezoneAbbreviation = "GMT",
            Elevation = 23.0,
            Current = TestData.ValidCurrentWeather,
            Daily = new DailyWeather { Time = [] } // Empty daily data
        };

        // Act
        var result = WeatherMapper.MapToPublicDto(external);

        // Assert
        result.Daily.Should().NotBeNull();
        result.Daily.Should().BeEmpty();
    }

    [Fact]
    public void MapToPublicDto_NullInput_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => WeatherMapper.MapToPublicDto(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void MapToPublicDto_DailyWeatherMapping_ConvertsUnitsCorrectly()
    {
        // Arrange
        var external = TestData.ValidWeatherResponse;

        // Act
        var result = WeatherMapper.MapToPublicDto(external);

        // Assert
        var firstDay = result.Daily.First();
        firstDay.Date.Should().Be("2024-01-15");
        firstDay.TemperatureMaxC.Should().Be(18.1);
        firstDay.TemperatureMinC.Should().Be(8.3);
        firstDay.PrecipitationProbabilityPct.Should().Be(20);
        firstDay.WindSpeedMaxKph.Should().Be(45.0); // 12.5 m/s * 3.6
        firstDay.WeatherCode.Should().Be(3);
        firstDay.Condition.Should().Be("Overcast");
        firstDay.Icon.Should().Be("04d");
    }

    [Fact]
    public void MapToPublicDto_IncompleteArrayData_HandlesGracefully()
    {
        // Arrange
        var incompleteDaily = new DailyWeather
        {
            Time = [DateOnly.Parse("2024-01-15"), DateOnly.Parse("2024-01-16")],
            WeatherCode = [3], // Only one weather code for two days
            TemperatureMax = [18.1], // Only one temperature for two days
            TemperatureMin = [], // Empty array
            PrecipitationProbabilityMax = [20, 80, 90], // More values than days
            WindSpeedMax = [12.5]
        };

        var external = new OpenMeteoWeatherResponse
        {
            Latitude = 51.5074,
            Longitude = -0.1278,
            Timezone = "Europe/London",
            TimezoneAbbreviation = "GMT",
            Elevation = 23.0,
            Current = TestData.ValidCurrentWeather,
            Daily = incompleteDaily
        };

        // Act
        var result = WeatherMapper.MapToPublicDto(external);

        // Assert
        result.Daily.Should().HaveCount(2); // Should create entries for all days

        // First day should have data
        var firstDay = result.Daily[0];
        firstDay.WeatherCode.Should().Be(3);
        firstDay.TemperatureMaxC.Should().Be(18.1);

        // Second day should use defaults for missing data
        var secondDay = result.Daily[1];
        secondDay.WeatherCode.Should().Be(0); // Default when index out of range
        secondDay.TemperatureMaxC.Should().Be(0.0); // Default when index out of range
        secondDay.TemperatureMinC.Should().Be(0.0); // Default when array is empty
        secondDay.PrecipitationProbabilityPct.Should().Be(80); // Should get second value
    }

    [Fact]
    public void MapToPublicDto_CurrentWeatherDayNightMapping_HandlesCorrectly()
    {
        // Arrange
        var nightWeather = new CurrentWeather
        {
            Time = DateTime.Parse("2024-01-15T02:30:00"),
            Temperature = 10.0,
            WindSpeed = 2.0,
            IsDay = 0, // Night
            WeatherCode = 1 // Mainly clear
        };

        var external = new OpenMeteoWeatherResponse
        {
            Latitude = 51.5074,
            Longitude = -0.1278,
            Timezone = "Europe/London",
            TimezoneAbbreviation = "GMT",
            Elevation = 23.0,
            Current = nightWeather,
            Daily = null
        };

        // Act
        var result = WeatherMapper.MapToPublicDto(external);

        // Assert
        result.Current.IsDay.Should().BeFalse();
        result.Current.Icon.Should().Be("02n"); // Night icon for mainly clear
        result.Current.Condition.Should().Be("Mainly clear");
    }

    [Theory]
    [InlineData(3.5, 12.6)]
    [InlineData(12.5, 45.0)]
    [InlineData(0.0, 0.0)]
    [InlineData(10.0, 36.0)]
    public void MapToPublicDto_WindSpeedConversion_ConvertsCorrectly(double mps, double expectedKmh)
    {
        // Arrange
        var current = new CurrentWeather
        {
            Time = DateTime.Parse("2024-01-15T14:30:00"),
            Temperature = 15.0,
            WindSpeed = mps,
            IsDay = 1,
            WeatherCode = 0
        };

        var external = new OpenMeteoWeatherResponse
        {
            Latitude = 51.5074,
            Longitude = -0.1278,
            Timezone = "Europe/London",
            TimezoneAbbreviation = "GMT",
            Elevation = 23.0,
            Current = current,
            Daily = null
        };

        // Act
        var result = WeatherMapper.MapToPublicDto(external);

        // Assert
        result.Current.WindSpeedKph.Should().Be(expectedKmh);
    }

    [Theory]
    [InlineData(15.123, 15.1)]
    [InlineData(15.167, 15.2)]
    [InlineData(0.0, 0.0)]
    [InlineData(-5.678, -5.7)]
    public void MapToPublicDto_TemperatureRounding_RoundsCorrectly(double input, double expected)
    {
        // Arrange
        var current = new CurrentWeather
        {
            Time = DateTime.Parse("2024-01-15T14:30:00"),
            Temperature = input,
            WindSpeed = 0.0,
            IsDay = 1,
            WeatherCode = 0
        };

        var external = new OpenMeteoWeatherResponse
        {
            Latitude = 51.5074,
            Longitude = -0.1278,
            Timezone = "Europe/London",
            TimezoneAbbreviation = "GMT",
            Elevation = 23.0,
            Current = current,
            Daily = null
        };

        // Act
        var result = WeatherMapper.MapToPublicDto(external);

        // Assert
        result.Current.TemperatureC.Should().Be(expected);
    }
}
