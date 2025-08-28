using FluentAssertions;
using WeatherProxyApi.Utils;
using Xunit;

namespace WeatherProxyApi.Tests.Unit.Utils;

/// <summary>
/// Unit tests for CoordinateValidator utility class
/// </summary>
public class CoordinateValidatorTests
{
    [Theory]
    [InlineData(0.0, 0.0)]
    [InlineData(51.5074, -0.1278)] // London
    [InlineData(50.0647, 19.9450)] // Kraków
    [InlineData(90.0, 180.0)] // Maximum valid values
    [InlineData(-90.0, -180.0)] // Minimum valid values
    [InlineData(45.0, 0.0)] // Equator crossing
    public void ValidateCoordinates_ValidInputs_ReturnsTrue(double latitude, double longitude)
    {
        // Act
        var result = CoordinateValidator.ValidateCoordinates(latitude, longitude);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData(91.0, 0.0)] // Latitude too high
    [InlineData(-91.0, 0.0)] // Latitude too low
    [InlineData(0.0, 181.0)] // Longitude too high
    [InlineData(0.0, -181.0)] // Longitude too low
    [InlineData(100.0, 200.0)] // Both invalid
    [InlineData(double.NaN, 0.0)] // NaN latitude
    [InlineData(0.0, double.NaN)] // NaN longitude
    [InlineData(double.PositiveInfinity, 0.0)] // Infinite latitude
    [InlineData(0.0, double.PositiveInfinity)] // Infinite longitude
    public void ValidateCoordinates_InvalidInputs_ReturnsFalse(double latitude, double longitude)
    {
        // Act
        var result = CoordinateValidator.ValidateCoordinates(latitude, longitude);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData(90.0)]
    [InlineData(-90.0)]
    [InlineData(0.0)]
    [InlineData(51.5074)]
    [InlineData(-25.7479)] // Cape Town
    public void IsValidLatitude_ValidLatitudes_ReturnsTrue(double latitude)
    {
        // Act
        var result = CoordinateValidator.IsValidLatitude(latitude);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData(90.1)]
    [InlineData(-90.1)]
    [InlineData(180.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void IsValidLatitude_InvalidLatitudes_ReturnsFalse(double latitude)
    {
        // Act
        var result = CoordinateValidator.IsValidLatitude(latitude);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData(180.0)]
    [InlineData(-180.0)]
    [InlineData(0.0)]
    [InlineData(-0.1278)]
    [InlineData(139.6917)] // Tokyo
    public void IsValidLongitude_ValidLongitudes_ReturnsTrue(double longitude)
    {
        // Act
        var result = CoordinateValidator.IsValidLongitude(longitude);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData(180.1)]
    [InlineData(-180.1)]
    [InlineData(360.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void IsValidLongitude_InvalidLongitudes_ReturnsFalse(double longitude)
    {
        // Act
        var result = CoordinateValidator.IsValidLongitude(longitude);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData(51.5074, -0.1278, false)] // London - meaningful
    [InlineData(50.0647, 19.9450, false)] // Kraków - meaningful
    [InlineData(40.7128, -74.0060, false)] // New York - meaningful
    public void ValidateMeaningfulCoordinates_MeaningfulCoordinates_ReturnsTrue(double latitude, double longitude, bool allowNullIsland)
    {
        // Act
        var result = CoordinateValidator.ValidateMeaningfulCoordinates(latitude, longitude, allowNullIsland);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData(0.0, 0.0)] // Null Island
    [InlineData(0.0001, 0.0001)] // Very close to null island
    [InlineData(-0.0001, -0.0001)] // Very close to null island
    public void ValidateMeaningfulCoordinates_NullIslandNotAllowed_ReturnsFalse(double latitude, double longitude)
    {
        // Act
        var result = CoordinateValidator.ValidateMeaningfulCoordinates(latitude, longitude, allowNullIsland: false);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData(0.0, 0.0)] // Null Island
    [InlineData(0.0001, 0.0001)] // Very close to null island
    [InlineData(-0.0001, -0.0001)] // Very close to null island
    public void ValidateMeaningfulCoordinates_NullIslandAllowed_ReturnsTrue(double latitude, double longitude)
    {
        // Act
        var result = CoordinateValidator.ValidateMeaningfulCoordinates(latitude, longitude, allowNullIsland: true);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData(91.0, 0.0)] // Invalid latitude
    [InlineData(0.0, 181.0)] // Invalid longitude
    [InlineData(double.NaN, 0.0)] // NaN latitude
    public void ValidateMeaningfulCoordinates_InvalidCoordinates_ReturnsFalse(double latitude, double longitude)
    {
        // Act
        var result = CoordinateValidator.ValidateMeaningfulCoordinates(latitude, longitude, allowNullIsland: true);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateCoordinates_EdgeCasePrecision_HandlesCorrectly()
    {
        // Arrange
        var almostValidLat = 90.000000001; // Slightly over limit
        var almostValidLon = 180.000000001; // Slightly over limit

        // Act
        var latResult = CoordinateValidator.IsValidLatitude(almostValidLat);
        var lonResult = CoordinateValidator.IsValidLongitude(almostValidLon);

        // Assert
        latResult.Should().BeFalse("Latitude slightly over 90 should be invalid");
        lonResult.Should().BeFalse("Longitude slightly over 180 should be invalid");
    }
}
