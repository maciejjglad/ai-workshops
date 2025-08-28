using FluentAssertions;
using WeatherProxyApi.Models.Requests;
using WeatherProxyApi.Tests.TestFixtures;
using WeatherProxyApi.Validation;
using Xunit;

namespace WeatherProxyApi.Tests.Unit.Validation;

/// <summary>
/// Unit tests for WeatherRequestValidator
/// </summary>
public class WeatherRequestValidatorTests
{
    private readonly WeatherRequestValidator _validator;

    public WeatherRequestValidatorTests()
    {
        _validator = new WeatherRequestValidator();
    }

    [Fact]
    public async Task ValidateAsync_ValidRequest_PassesValidation()
    {
        // Arrange
        var request = TestData.ValidWeatherRequest;

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData(90.0, 180.0)] // Maximum valid values
    [InlineData(-90.0, -180.0)] // Minimum valid values
    [InlineData(0.0, 0.0)] // Equator and prime meridian
    [InlineData(51.5074, -0.1278)] // London
    [InlineData(50.0647, 19.9450)] // Kraków
    public async Task ValidateAsync_ValidCoordinates_PassValidation(double lat, double lon)
    {
        // Arrange
        var request = new WeatherRequest
        {
            Lat = lat,
            Lon = lon,
            Days = 5
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(90.1)] // Too high
    [InlineData(-90.1)] // Too low
    [InlineData(100.0)] // Way too high
    [InlineData(-100.0)] // Way too low
    public async Task ValidateAsync_InvalidLatitude_FailsValidation(double invalidLat)
    {
        // Arrange
        var request = new WeatherRequest
        {
            Lat = invalidLat,
            Lon = 0.0,
            Days = 5
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => 
            e.PropertyName == nameof(WeatherRequest.Lat) &&
            e.ErrorMessage == "Latitude must be between -90 and 90");
    }

    [Theory]
    [InlineData(180.1)] // Too high
    [InlineData(-180.1)] // Too low
    [InlineData(200.0)] // Way too high
    [InlineData(-200.0)] // Way too low
    public async Task ValidateAsync_InvalidLongitude_FailsValidation(double invalidLon)
    {
        // Arrange
        var request = new WeatherRequest
        {
            Lat = 0.0,
            Lon = invalidLon,
            Days = 5
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => 
            e.PropertyName == nameof(WeatherRequest.Lon) &&
            e.ErrorMessage == "Longitude must be between -180 and 180");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(7)]
    public async Task ValidateAsync_ValidDays_PassValidation(int days)
    {
        // Arrange
        var request = new WeatherRequest
        {
            Lat = 51.5074,
            Lon = -0.1278,
            Days = days
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-5)]
    public async Task ValidateAsync_DaysZeroOrNegative_FailsValidation(int invalidDays)
    {
        // Arrange
        var request = new WeatherRequest
        {
            Lat = 51.5074,
            Lon = -0.1278,
            Days = invalidDays
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => 
            e.PropertyName == nameof(WeatherRequest.Days) &&
            e.ErrorMessage == "Days must be greater than 0");
    }

    [Theory]
    [InlineData(8)]
    [InlineData(10)]
    [InlineData(15)]
    public async Task ValidateAsync_DaysTooHigh_FailsValidation(int tooManyDays)
    {
        // Arrange
        var request = new WeatherRequest
        {
            Lat = 51.5074,
            Lon = -0.1278,
            Days = tooManyDays
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => 
            e.PropertyName == nameof(WeatherRequest.Days) &&
            e.ErrorMessage == "Days cannot exceed 7");
    }

    [Fact]
    public async Task ValidateAsync_MultipleValidationErrors_ReportsAllErrors()
    {
        // Arrange
        var request = new WeatherRequest
        {
            Lat = 91.0, // Invalid latitude
            Lon = 181.0, // Invalid longitude
            Days = 0 // Invalid days
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(3);
        result.Errors.Should().Contain(e => e.PropertyName == nameof(WeatherRequest.Lat));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(WeatherRequest.Lon));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(WeatherRequest.Days));
    }

    [Fact]
    public async Task ValidateAsync_EdgeCaseCoordinates_PassValidation()
    {
        // Arrange - Test exact boundary values
        var request = new WeatherRequest
        {
            Lat = 90.0, // Exact maximum
            Lon = -180.0, // Exact minimum
            Days = 1 // Minimum valid days
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAsync_AnotherEdgeCaseCoordinates_PassValidation()
    {
        // Arrange - Test other exact boundary values
        var request = new WeatherRequest
        {
            Lat = -90.0, // Exact minimum
            Lon = 180.0, // Exact maximum
            Days = 7 // Maximum valid days
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(double.NaN, 0.0)]
    [InlineData(0.0, double.NaN)]
    [InlineData(double.PositiveInfinity, 0.0)]
    [InlineData(0.0, double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity, 0.0)]
    [InlineData(0.0, double.NegativeInfinity)]
    public async Task ValidateAsync_SpecialFloatingPointValues_FailsValidation(double lat, double lon)
    {
        // Arrange
        var request = new WeatherRequest
        {
            Lat = lat,
            Lon = lon,
            Days = 5
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty("NaN or infinite values should fail validation");
    }
}
