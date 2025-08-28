using FluentAssertions;
using WeatherProxyApi.Utils;
using Xunit;

namespace WeatherProxyApi.Tests.Unit.Utils;

/// <summary>
/// Unit tests for CountryCodeMapper utility class
/// </summary>
public class CountryCodeMapperTests
{
    [Theory]
    [InlineData("US", "United States")]
    [InlineData("CA", "Canada")]
    [InlineData("GB", "United Kingdom")]
    [InlineData("DE", "Germany")]
    [InlineData("FR", "France")]
    [InlineData("PL", "Poland")]
    [InlineData("ES", "Spain")]
    [InlineData("IT", "Italy")]
    public void GetCountryName_CommonCountryCodes_ReturnsCorrectNames(string countryCode, string expectedName)
    {
        // Act
        var result = CountryCodeMapper.GetCountryName(countryCode);

        // Assert
        result.Should().Be(expectedName);
    }

    [Theory]
    [InlineData("us", "United States")] // Lowercase
    [InlineData("gb", "United Kingdom")] // Lowercase
    [InlineData("Us", "United States")] // Mixed case
    [InlineData("Gb", "United Kingdom")] // Mixed case
    public void GetCountryName_DifferentCasing_ReturnsCorrectNames(string countryCode, string expectedName)
    {
        // Act
        var result = CountryCodeMapper.GetCountryName(countryCode);

        // Assert
        result.Should().Be(expectedName);
    }

    [Theory]
    [InlineData("XX")]
    [InlineData("ZZ")]
    [InlineData("123")]
    [InlineData("ABC")] // Too long
    [InlineData("X")] // Too short
    public void GetCountryName_UnknownCountryCodes_ReturnsOriginalCode(string unknownCode)
    {
        // Act
        var result = CountryCodeMapper.GetCountryName(unknownCode);

        // Assert
        result.Should().Be(unknownCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("  ")]
    public void GetCountryName_EmptyOrWhitespaceInput_ReturnsOriginalInput(string input)
    {
        // Act
        var result = CountryCodeMapper.GetCountryName(input);

        // Assert
        result.Should().Be(input);
    }

    [Theory]
    [InlineData("JP", "Japan")]
    [InlineData("KR", "South Korea")]
    [InlineData("CN", "China")]
    [InlineData("IN", "India")]
    [InlineData("AU", "Australia")]
    [InlineData("NZ", "New Zealand")]
    [InlineData("BR", "Brazil")]
    [InlineData("AR", "Argentina")]
    [InlineData("MX", "Mexico")]
    [InlineData("RU", "Russia")]
    public void GetCountryName_AdditionalCountryCodes_ReturnsCorrectNames(string countryCode, string expectedName)
    {
        // Act
        var result = CountryCodeMapper.GetCountryName(countryCode);

        // Assert
        result.Should().Be(expectedName);
    }

    [Theory]
    [InlineData("NL", "Netherlands")]
    [InlineData("BE", "Belgium")]
    [InlineData("AT", "Austria")]
    [InlineData("CH", "Switzerland")]
    [InlineData("CZ", "Czech Republic")]
    [InlineData("DK", "Denmark")]
    [InlineData("SE", "Sweden")]
    [InlineData("NO", "Norway")]
    [InlineData("FI", "Finland")]
    [InlineData("IE", "Ireland")]
    public void GetCountryName_EuropeanCountryCodes_ReturnsCorrectNames(string countryCode, string expectedName)
    {
        // Act
        var result = CountryCodeMapper.GetCountryName(countryCode);

        // Assert
        result.Should().Be(expectedName);
    }

    [Theory]
    [InlineData("PT", "Portugal")]
    [InlineData("GR", "Greece")]
    [InlineData("HU", "Hungary")]
    [InlineData("SK", "Slovakia")]
    [InlineData("SI", "Slovenia")]
    [InlineData("HR", "Croatia")]
    [InlineData("BG", "Bulgaria")]
    [InlineData("RO", "Romania")]
    [InlineData("LT", "Lithuania")]
    [InlineData("LV", "Latvia")]
    [InlineData("EE", "Estonia")]
    [InlineData("LU", "Luxembourg")]
    [InlineData("MT", "Malta")]
    [InlineData("CY", "Cyprus")]
    public void GetCountryName_ExtendedEuropeanCountryCodes_ReturnsCorrectNames(string countryCode, string expectedName)
    {
        // Act
        var result = CountryCodeMapper.GetCountryName(countryCode);

        // Assert
        result.Should().Be(expectedName);
    }

    [Fact]
    public void GetCountryName_AllMappedCodes_AreValidIso2Codes()
    {
        // Arrange
        var validIsoCodes = new[]
        {
            "US", "CA", "GB", "DE", "FR", "PL", "ES", "IT", "NL", "BE", "AT", "CH",
            "CZ", "DK", "SE", "NO", "FI", "IE", "PT", "GR", "HU", "SK", "SI", "HR",
            "BG", "RO", "LT", "LV", "EE", "LU", "MT", "CY", "AU", "NZ", "JP", "KR",
            "CN", "IN", "BR", "AR", "MX", "RU"
        };

        foreach (var code in validIsoCodes)
        {
            // Act
            var result = CountryCodeMapper.GetCountryName(code);

            // Assert
            result.Should().NotBe(code, $"Country code {code} should be mapped to a country name");
            result.Should().NotBeNullOrEmpty($"Country code {code} should map to a non-empty name");
            result.Length.Should().BeGreaterThan(2, $"Country name for {code} should be longer than the code itself");
        }
    }

    [Fact]
    public void GetCountryName_CaseInsensitivity_WorksConsistently()
    {
        // Arrange
        var testCodes = new[] { "US", "GB", "DE", "PL" };

        foreach (var code in testCodes)
        {
            // Act
            var upperResult = CountryCodeMapper.GetCountryName(code.ToUpperInvariant());
            var lowerResult = CountryCodeMapper.GetCountryName(code.ToLowerInvariant());

            // Assert
            upperResult.Should().Be(lowerResult, $"Case should not matter for country code {code}");
        }
    }
}
