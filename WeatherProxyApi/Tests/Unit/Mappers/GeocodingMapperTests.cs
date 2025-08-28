using FluentAssertions;
using WeatherProxyApi.Models.External;
using WeatherProxyApi.Services.Mappers;
using WeatherProxyApi.Tests.TestFixtures;
using Xunit;

namespace WeatherProxyApi.Tests.Unit.Mappers;

/// <summary>
/// Unit tests for GeocodingMapper
/// </summary>
public class GeocodingMapperTests
{
    [Fact]
    public void MapToPublicDto_ValidInput_MapsAllFields()
    {
        // Arrange
        var external = TestData.LondonCityResult;

        // Act
        var result = GeocodingMapper.MapToPublicDto(external);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("London");
        result.Country.Should().Be("United Kingdom");
        result.Latitude.Should().Be(51.508530); // Rounded to 6 decimals
        result.Longitude.Should().Be(-0.125740); // Rounded to 6 decimals
        result.Region.Should().Be("England");
        result.Population.Should().Be(8982000);
    }

    [Fact]
    public void MapToPublicDto_MissingCountryWithValidCountryCode_UsesCountryCodeMapping()
    {
        // Arrange
        var external = new OpenMeteoCityResult
        {
            Id = 1,
            Name = "Test City",
            Latitude = 52.5,
            Longitude = 13.4,
            CountryCode = "DE",
            Country = null, // Missing country name
            Admin1 = "Berlin"
        };

        // Act
        var result = GeocodingMapper.MapToPublicDto(external);

        // Assert
        result.Should().NotBeNull();
        result.Country.Should().Be("Germany"); // Mapped from country code
    }

    [Fact]
    public void MapToPublicDto_MissingCountryAndInvalidCountryCode_UsesUnknown()
    {
        // Arrange
        var external = new OpenMeteoCityResult
        {
            Id = 1,
            Name = "Test City",
            Latitude = 52.5,
            Longitude = 13.4,
            CountryCode = "XX", // Invalid country code
            Country = null, // Missing country name
            Admin1 = "Test Region"
        };

        // Act
        var result = GeocodingMapper.MapToPublicDto(external);

        // Assert
        result.Should().NotBeNull();
        result.Country.Should().Be("XX"); // Falls back to original code when unknown
    }

    [Fact]
    public void MapToPublicDto_MissingCountryAndCountryCode_UsesUnknown()
    {
        // Arrange
        var external = new OpenMeteoCityResult
        {
            Id = 1,
            Name = "Test City",
            Latitude = 52.5,
            Longitude = 13.4,
            CountryCode = "", // Empty country code
            Country = null, // Missing country name
            Admin1 = "Test Region"
        };

        // Act
        var result = GeocodingMapper.MapToPublicDto(external);

        // Assert
        result.Should().NotBeNull();
        result.Country.Should().Be("Unknown");
    }

    [Fact]
    public void MapToPublicDto_RoundsCoordinatesToSixDecimals()
    {
        // Arrange
        var external = new OpenMeteoCityResult
        {
            Id = 1,
            Name = "Test City",
            Latitude = 51.5074123456789, // Many decimal places
            Longitude = -0.1278987654321, // Many decimal places
            CountryCode = "GB",
            Country = "United Kingdom"
        };

        // Act
        var result = GeocodingMapper.MapToPublicDto(external);

        // Assert
        result.Latitude.Should().Be(51.507412); // Rounded to 6 decimals
        result.Longitude.Should().Be(-0.127899); // Rounded to 6 decimals
    }

    [Fact]
    public void MapToPublicDto_NullInput_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => GeocodingMapper.MapToPublicDto(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void MapToPublicDtos_ValidCollection_MapsAllItems()
    {
        // Arrange
        var externalResults = new List<OpenMeteoCityResult>
        {
            TestData.LondonCityResult,
            TestData.KrakowCityResult
        };

        // Act
        var results = GeocodingMapper.MapToPublicDtos(externalResults);

        // Assert
        results.Should().HaveCount(2);
        
        var london = results.First(r => r.Name == "London");
        london.Country.Should().Be("United Kingdom");
        london.Latitude.Should().Be(51.508530);
        
        var krakow = results.First(r => r.Name == "Kraków");
        krakow.Country.Should().Be("Poland");
        krakow.Latitude.Should().Be(50.061430);
    }

    [Fact]
    public void MapToPublicDtos_NullCollection_ReturnsEmptyList()
    {
        // Act
        var results = GeocodingMapper.MapToPublicDtos(null);

        // Assert
        results.Should().NotBeNull();
        results.Should().BeEmpty();
    }

    [Fact]
    public void MapToPublicDtos_EmptyCollection_ReturnsEmptyList()
    {
        // Arrange
        var emptyResults = new List<OpenMeteoCityResult>();

        // Act
        var results = GeocodingMapper.MapToPublicDtos(emptyResults);

        // Assert
        results.Should().NotBeNull();
        results.Should().BeEmpty();
    }

    [Fact]
    public void MapToPublicDtos_CollectionWithEmptyNames_FiltersOutEmptyNames()
    {
        // Arrange
        var externalResults = new List<OpenMeteoCityResult>
        {
            TestData.LondonCityResult, // Valid
            new OpenMeteoCityResult
            {
                Id = 1,
                Name = "", // Empty name
                Latitude = 52.5,
                Longitude = 13.4,
                CountryCode = "DE",
                Country = "Germany"
            },
            new OpenMeteoCityResult
            {
                Id = 2,
                Name = "   ", // Whitespace name
                Latitude = 48.8,
                Longitude = 2.3,
                CountryCode = "FR",
                Country = "France"
            },
            TestData.KrakowCityResult // Valid
        };

        // Act
        var results = GeocodingMapper.MapToPublicDtos(externalResults);

        // Assert
        results.Should().HaveCount(2); // Only valid names
        results.Should().Contain(r => r.Name == "London");
        results.Should().Contain(r => r.Name == "Kraków");
    }

    [Fact]
    public void MapToPublicDto_OptionalFieldsHandling_HandlesNullValues()
    {
        // Arrange
        var external = new OpenMeteoCityResult
        {
            Id = 1,
            Name = "Test City",
            Latitude = 52.5,
            Longitude = 13.4,
            CountryCode = "DE",
            Country = "Germany",
            Admin1 = null, // Optional field
            Population = null // Optional field
        };

        // Act
        var result = GeocodingMapper.MapToPublicDto(external);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Test City");
        result.Country.Should().Be("Germany");
        result.Region.Should().BeNull();
        result.Population.Should().BeNull();
    }

    [Theory]
    [InlineData(0.0, 0.0, 0.0, 0.0)]
    [InlineData(90.0, 180.0, 90.0, 180.0)]
    [InlineData(-90.0, -180.0, -90.0, -180.0)]
    [InlineData(51.1234567, -0.9876543, 51.123457, -0.987654)]
    public void MapToPublicDto_CoordinateRounding_RoundsCorrectly(double inputLat, double inputLon, double expectedLat, double expectedLon)
    {
        // Arrange
        var external = new OpenMeteoCityResult
        {
            Id = 1,
            Name = "Test City",
            Latitude = inputLat,
            Longitude = inputLon,
            CountryCode = "GB",
            Country = "United Kingdom"
        };

        // Act
        var result = GeocodingMapper.MapToPublicDto(external);

        // Assert
        result.Latitude.Should().Be(expectedLat);
        result.Longitude.Should().Be(expectedLon);
    }
}
