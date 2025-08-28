using FluentAssertions;
using WeatherProxyApi.Models.Requests;
using WeatherProxyApi.Tests.TestFixtures;
using WeatherProxyApi.Validation;
using Xunit;

namespace WeatherProxyApi.Tests.Unit.Validation;

/// <summary>
/// Unit tests for CitySearchRequestValidator
/// </summary>
public class CitySearchRequestValidatorTests
{
    private readonly CitySearchRequestValidator _validator;

    public CitySearchRequestValidatorTests()
    {
        _validator = new CitySearchRequestValidator();
    }

    [Fact]
    public async Task ValidateAsync_ValidRequest_PassesValidation()
    {
        // Arrange
        var request = TestData.ValidCitySearchRequest;

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateAsync_KrakowRequest_PassesValidation()
    {
        // Arrange
        var request = TestData.KrakowSearchRequest;

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("  ")]
    public async Task ValidateAsync_EmptyOrWhitespaceQuery_FailsValidation(string query)
    {
        // Arrange
        var request = new CitySearchRequest
        {
            Q = query,
            Count = 5,
            Language = "en"
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => 
            e.PropertyName == nameof(CitySearchRequest.Q) &&
            e.ErrorMessage == "Search query is required");
    }

    [Fact]
    public async Task ValidateAsync_QueryTooShort_FailsValidation()
    {
        // Arrange
        var request = new CitySearchRequest
        {
            Q = "A", // Only 1 character
            Count = 5,
            Language = "en"
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => 
            e.PropertyName == nameof(CitySearchRequest.Q) &&
            e.ErrorMessage == "Search query must be at least 2 characters");
    }

    [Fact]
    public async Task ValidateAsync_QueryTooLong_FailsValidation()
    {
        // Arrange
        var request = new CitySearchRequest
        {
            Q = new string('A', 101), // 101 characters
            Count = 5,
            Language = "en"
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => 
            e.PropertyName == nameof(CitySearchRequest.Q) &&
            e.ErrorMessage == "Search query cannot exceed 100 characters");
    }

    [Theory]
    [InlineData("City@Name")]
    [InlineData("City#Name")]
    [InlineData("City$Name")]
    [InlineData("City%Name")]
    [InlineData("City&Name")]
    [InlineData("City*Name")]
    [InlineData("City+Name")]
    [InlineData("City=Name")]
    public async Task ValidateAsync_QueryWithInvalidCharacters_FailsValidation(string invalidQuery)
    {
        // Arrange
        var request = new CitySearchRequest
        {
            Q = invalidQuery,
            Count = 5,
            Language = "en"
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => 
            e.PropertyName == nameof(CitySearchRequest.Q) &&
            e.ErrorMessage == "Search query contains invalid characters");
    }

    [Theory]
    [InlineData("New York")] // Space
    [InlineData("Saint-Denis")] // Hyphen
    [InlineData("O'Brien")] // Apostrophe
    [InlineData("São Paulo")] // Unicode characters
    [InlineData("München")] // Umlaut
    [InlineData("Москва")] // Cyrillic
    [InlineData("東京")] // Japanese
    [InlineData("القاهرة")] // Arabic
    public async Task ValidateAsync_QueryWithValidSpecialCharacters_PassesValidation(string validQuery)
    {
        // Arrange
        var request = new CitySearchRequest
        {
            Q = validQuery,
            Count = 5,
            Language = "en"
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue($"Query '{validQuery}' should be valid");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-5)]
    public async Task ValidateAsync_CountZeroOrNegative_FailsValidation(int count)
    {
        // Arrange
        var request = new CitySearchRequest
        {
            Q = "London",
            Count = count,
            Language = "en"
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => 
            e.PropertyName == nameof(CitySearchRequest.Count) &&
            e.ErrorMessage == "Count must be greater than 0");
    }

    [Fact]
    public async Task ValidateAsync_CountTooHigh_FailsValidation()
    {
        // Arrange
        var request = new CitySearchRequest
        {
            Q = "London",
            Count = 11, // Maximum is 10
            Language = "en"
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => 
            e.PropertyName == nameof(CitySearchRequest.Count) &&
            e.ErrorMessage == "Count cannot exceed 10");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    public async Task ValidateAsync_ValidCounts_PassValidation(int count)
    {
        // Arrange
        var request = new CitySearchRequest
        {
            Q = "London",
            Count = count,
            Language = "en"
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("eng")] // Too long
    [InlineData("e")] // Too short
    [InlineData("E1")] // Contains number
    [InlineData("EN")] // Uppercase
    [InlineData("e-")] // Contains special character
    public async Task ValidateAsync_InvalidLanguageCode_FailsValidation(string language)
    {
        // Arrange
        var request = new CitySearchRequest
        {
            Q = "London",
            Count = 5,
            Language = language
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => 
            e.PropertyName == nameof(CitySearchRequest.Language) &&
            e.ErrorMessage == "Language must be a valid 2-letter ISO code");
    }

    [Theory]
    [InlineData("en")]
    [InlineData("pl")]
    [InlineData("fr")]
    [InlineData("de")]
    [InlineData("es")]
    public async Task ValidateAsync_ValidLanguageCodes_PassValidation(string language)
    {
        // Arrange
        var request = new CitySearchRequest
        {
            Q = "London",
            Count = 5,
            Language = language
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAsync_EmptyLanguage_PassesValidation()
    {
        // Arrange
        var request = new CitySearchRequest
        {
            Q = "London",
            Count = 5,
            Language = "" // Empty should be allowed (uses default)
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAsync_MultipleValidationErrors_ReportsAllErrors()
    {
        // Arrange
        var request = new CitySearchRequest
        {
            Q = "A", // Too short
            Count = 0, // Invalid count
            Language = "invalid" // Invalid language
        };

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(3);
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CitySearchRequest.Q));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CitySearchRequest.Count));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CitySearchRequest.Language));
    }
}
