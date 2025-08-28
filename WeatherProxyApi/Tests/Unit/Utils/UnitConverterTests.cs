using FluentAssertions;
using WeatherProxyApi.Utils;
using Xunit;

namespace WeatherProxyApi.Tests.Unit.Utils;

/// <summary>
/// Unit tests for UnitConverter utility class
/// </summary>
public class UnitConverterTests
{
    [Theory]
    [InlineData(0.0, 0.0)]
    [InlineData(1.0, 3.6)]
    [InlineData(5.0, 18.0)]
    [InlineData(10.0, 36.0)]
    [InlineData(27.78, 100.0)] // ~100 km/h
    public void ConvertWindSpeedToKmh_ValidInputs_ReturnsCorrectConversion(double mps, double expectedKmh)
    {
        // Act
        var result = UnitConverter.ConvertWindSpeedToKmh(mps);

        // Assert
        result.Should().BeApproximately(expectedKmh, 0.1);
    }

    [Theory]
    [InlineData(3.5, 12.6)] // From test data
    [InlineData(12.5, 45.0)] // From test data
    [InlineData(15.8, 56.9)] // From test data
    public void ConvertWindSpeedToKmh_RealWorldValues_ReturnsRoundedResults(double mps, double expectedKmh)
    {
        // Act
        var result = UnitConverter.ConvertWindSpeedToKmh(mps);

        // Assert
        result.Should().Be(expectedKmh);
    }

    [Fact]
    public void ConvertWindSpeedToKmh_NegativeInput_ReturnsNegativeResult()
    {
        // Arrange
        var negativeMps = -5.0;

        // Act
        var result = UnitConverter.ConvertWindSpeedToKmh(negativeMps);

        // Assert
        result.Should().Be(-18.0);
    }

    [Theory]
    [InlineData(0.0, 32.0)]
    [InlineData(100.0, 212.0)]
    [InlineData(-40.0, -40.0)] // Special point where C and F are equal
    [InlineData(15.2, 59.4)] // From test data
    public void ConvertToFahrenheit_ValidInputs_ReturnsCorrectConversion(double celsius, double expectedFahrenheit)
    {
        // Act
        var result = UnitConverter.ConvertToFahrenheit(celsius);

        // Assert
        result.Should().BeApproximately(expectedFahrenheit, 0.1);
    }

    [Theory]
    [InlineData(20.5, 68.9)]
    [InlineData(-10.3, 13.5)]
    [InlineData(37.0, 98.6)] // Body temperature
    public void ConvertToFahrenheit_RealWorldValues_ReturnsRoundedResults(double celsius, double expectedFahrenheit)
    {
        // Act
        var result = UnitConverter.ConvertToFahrenheit(celsius);

        // Assert
        result.Should().Be(expectedFahrenheit);
    }

    [Theory]
    [InlineData(double.MaxValue)]
    [InlineData(double.MinValue)]
    [InlineData(1000000.0)]
    [InlineData(-1000000.0)]
    public void ConvertWindSpeedToKmh_ExtremeValues_HandlesGracefully(double extremeValue)
    {
        // Act
        var result = UnitConverter.ConvertWindSpeedToKmh(extremeValue);

        // Assert
        result.Should().NotBe(double.NaN);
        result.Should().NotBe(double.PositiveInfinity);
        result.Should().NotBe(double.NegativeInfinity);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void ConvertWindSpeedToKmh_InvalidValues_ReturnsInvalidResult(double invalidValue)
    {
        // Act
        var result = UnitConverter.ConvertWindSpeedToKmh(invalidValue);

        // Assert
        (double.IsNaN(result) || double.IsInfinity(result)).Should().BeTrue();
    }
}
