using FluentAssertions;
using WeatherProxyApi.Utils;
using Xunit;

namespace WeatherProxyApi.Tests.Unit.Utils;

/// <summary>
/// Unit tests for WeatherCodeMapper utility class
/// </summary>
public class WeatherCodeMapperTests
{
    [Theory]
    [InlineData(0, true, "Clear sky", "01d")]
    [InlineData(0, false, "Clear sky", "01n")]
    [InlineData(1, true, "Mainly clear", "02d")]
    [InlineData(1, false, "Mainly clear", "02n")]
    [InlineData(2, true, "Partly cloudy", "03d")]
    [InlineData(2, false, "Partly cloudy", "03n")]
    [InlineData(3, true, "Overcast", "04d")]
    [InlineData(3, false, "Overcast", "04d")] // Overcast doesn't change with day/night
    public void GetWeatherInfo_ClearSkyCodes_ReturnsCorrectMapping(int code, bool isDay, string expectedCondition, string expectedIcon)
    {
        // Act
        var (condition, icon) = WeatherCodeMapper.GetWeatherInfo(code, isDay);

        // Assert
        condition.Should().Be(expectedCondition);
        icon.Should().Be(expectedIcon);
    }

    [Theory]
    [InlineData(45, "Fog", "50d")]
    [InlineData(48, "Fog", "50d")]
    public void GetWeatherInfo_FogCodes_ReturnsCorrectMapping(int code, string expectedCondition, string expectedIcon)
    {
        // Act
        var (condition, icon) = WeatherCodeMapper.GetWeatherInfo(code, true);

        // Assert
        condition.Should().Be(expectedCondition);
        icon.Should().Be(expectedIcon);
    }

    [Theory]
    [InlineData(51, "Drizzle", "09d")]
    [InlineData(53, "Drizzle", "09d")]
    [InlineData(55, "Drizzle", "09d")]
    public void GetWeatherInfo_DrizzleCodes_ReturnsCorrectMapping(int code, string expectedCondition, string expectedIcon)
    {
        // Act
        var (condition, icon) = WeatherCodeMapper.GetWeatherInfo(code, true);

        // Assert
        condition.Should().Be(expectedCondition);
        icon.Should().Be(expectedIcon);
    }

    [Theory]
    [InlineData(61, "Rain", "10d")]
    [InlineData(63, "Rain", "10d")]
    [InlineData(65, "Rain", "10d")]
    public void GetWeatherInfo_RainCodes_ReturnsCorrectMapping(int code, string expectedCondition, string expectedIcon)
    {
        // Act
        var (condition, icon) = WeatherCodeMapper.GetWeatherInfo(code, true);

        // Assert
        condition.Should().Be(expectedCondition);
        icon.Should().Be(expectedIcon);
    }

    [Theory]
    [InlineData(71, "Snow", "13d")]
    [InlineData(73, "Snow", "13d")]
    [InlineData(75, "Snow", "13d")]
    public void GetWeatherInfo_SnowCodes_ReturnsCorrectMapping(int code, string expectedCondition, string expectedIcon)
    {
        // Act
        var (condition, icon) = WeatherCodeMapper.GetWeatherInfo(code, true);

        // Assert
        condition.Should().Be(expectedCondition);
        icon.Should().Be(expectedIcon);
    }

    [Theory]
    [InlineData(80, "Rain showers", "09d")]
    [InlineData(81, "Rain showers", "09d")]
    [InlineData(82, "Rain showers", "09d")]
    public void GetWeatherInfo_RainShowerCodes_ReturnsCorrectMapping(int code, string expectedCondition, string expectedIcon)
    {
        // Act
        var (condition, icon) = WeatherCodeMapper.GetWeatherInfo(code, true);

        // Assert
        condition.Should().Be(expectedCondition);
        icon.Should().Be(expectedIcon);
    }

    [Theory]
    [InlineData(85, "Snow showers", "13d")]
    [InlineData(86, "Snow showers", "13d")]
    public void GetWeatherInfo_SnowShowerCodes_ReturnsCorrectMapping(int code, string expectedCondition, string expectedIcon)
    {
        // Act
        var (condition, icon) = WeatherCodeMapper.GetWeatherInfo(code, true);

        // Assert
        condition.Should().Be(expectedCondition);
        icon.Should().Be(expectedIcon);
    }

    [Theory]
    [InlineData(95, "Thunderstorm", "11d")]
    [InlineData(96, "Thunderstorm with hail", "11d")]
    [InlineData(99, "Thunderstorm with hail", "11d")]
    public void GetWeatherInfo_ThunderstormCodes_ReturnsCorrectMapping(int code, string expectedCondition, string expectedIcon)
    {
        // Act
        var (condition, icon) = WeatherCodeMapper.GetWeatherInfo(code, true);

        // Assert
        condition.Should().Be(expectedCondition);
        icon.Should().Be(expectedIcon);
    }

    [Theory]
    [InlineData(999, true, "Unknown", "01d")]
    [InlineData(-1, true, "Unknown", "01d")]
    [InlineData(100, false, "Unknown", "01n")]
    [InlineData(4, true, "Unknown", "01d")]
    public void GetWeatherInfo_UnknownCodes_ReturnsUnknownWithDayNightIcon(int code, bool isDay, string expectedCondition, string expectedIcon)
    {
        // Act
        var (condition, icon) = WeatherCodeMapper.GetWeatherInfo(code, isDay);

        // Assert
        condition.Should().Be(expectedCondition);
        icon.Should().Be(expectedIcon);
    }

    [Fact]
    public void GetWeatherInfo_AllDefinedCodes_ReturnNonEmptyResults()
    {
        // Arrange
        var definedCodes = new[]
        {
            0, 1, 2, 3, 45, 48, 51, 53, 55, 61, 63, 65,
            71, 73, 75, 80, 81, 82, 85, 86, 95, 96, 99
        };

        foreach (var code in definedCodes)
        {
            foreach (var isDay in new[] { true, false })
            {
                // Act
                var (condition, icon) = WeatherCodeMapper.GetWeatherInfo(code, isDay);

                // Assert
                condition.Should().NotBeNullOrEmpty($"Code {code} with isDay={isDay} should have a condition");
                icon.Should().NotBeNullOrEmpty($"Code {code} with isDay={isDay} should have an icon");
                icon.Should().MatchRegex(@"^\d{2}[dn]$", $"Icon {icon} should match pattern '##d' or '##n'");
            }
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void GetWeatherInfo_DayNightSensitiveCodes_ReturnDifferentIcons(int code)
    {
        // Act
        var (dayCondition, dayIcon) = WeatherCodeMapper.GetWeatherInfo(code, true);
        var (nightCondition, nightIcon) = WeatherCodeMapper.GetWeatherInfo(code, false);

        // Assert
        dayCondition.Should().Be(nightCondition, "Condition should be the same for day and night");
        dayIcon.Should().NotBe(nightIcon, "Icons should differ between day and night for light-sensitive codes");
        dayIcon.Should().EndWith("d", "Day icon should end with 'd'");
        nightIcon.Should().EndWith("n", "Night icon should end with 'n'");
    }

    [Theory]
    [InlineData(3)] // Overcast
    [InlineData(45)] // Fog
    [InlineData(61)] // Rain
    [InlineData(95)] // Thunderstorm
    public void GetWeatherInfo_NonDayNightSensitiveCodes_ReturnSameIcons(int code)
    {
        // Act
        var (dayCondition, dayIcon) = WeatherCodeMapper.GetWeatherInfo(code, true);
        var (nightCondition, nightIcon) = WeatherCodeMapper.GetWeatherInfo(code, false);

        // Assert
        dayCondition.Should().Be(nightCondition, "Condition should be the same for day and night");
        dayIcon.Should().Be(nightIcon, "Icons should be the same for weather that doesn't depend on day/night");
    }
}
