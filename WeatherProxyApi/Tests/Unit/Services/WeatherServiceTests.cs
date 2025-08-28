using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using WeatherProxyApi.Models.Requests;
using WeatherProxyApi.Services;
using WeatherProxyApi.Services.Exceptions;
using WeatherProxyApi.Tests.TestFixtures;
using Xunit;

namespace WeatherProxyApi.Tests.Unit.Services;

/// <summary>
/// Unit tests for WeatherService
/// </summary>
public class WeatherServiceTests
{
    private readonly IWeatherApiClient _mockApiClient;
    private readonly ILogger<WeatherService> _mockLogger;
    private readonly WeatherService _service;

    public WeatherServiceTests()
    {
        _mockApiClient = Substitute.For<IWeatherApiClient>();
        _mockLogger = Substitute.For<ILogger<WeatherService>>();
        _service = new WeatherService(_mockApiClient, _mockLogger);
    }

    #region SearchCitiesAsync Tests

    [Fact]
    public async Task SearchCitiesAsync_ValidRequest_ReturnsCities()
    {
        // Arrange
        var request = TestData.ValidCitySearchRequest;
        _mockApiClient.GetGeocodingAsync(request.Q, request.Count, request.Language, Arg.Any<CancellationToken>())
            .Returns(TestData.ValidGeocodingResponse);

        // Act
        var result = await _service.SearchCitiesAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        
        var london = result.First(c => c.Name == "London");
        london.Country.Should().Be("United Kingdom");
        london.Latitude.Should().Be(51.508530);
        london.Longitude.Should().Be(-0.125740);
    }

    [Fact]
    public async Task SearchCitiesAsync_EmptyResults_ReturnsEmptyList()
    {
        // Arrange
        var request = TestData.ValidCitySearchRequest;
        _mockApiClient.GetGeocodingAsync(request.Q, request.Count, request.Language, Arg.Any<CancellationToken>())
            .Returns(TestData.EmptyGeocodingResponse);

        // Act
        var result = await _service.SearchCitiesAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchCitiesAsync_NullRequest_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = async () => await _service.SearchCitiesAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task SearchCitiesAsync_ExternalApiException_ThrowsInvalidOperationException()
    {
        // Arrange
        var request = TestData.ValidCitySearchRequest;
        _mockApiClient.GetGeocodingAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new ExternalApiException("API Error", 500));

        // Act & Assert
        var act = async () => await _service.SearchCitiesAsync(request);
        var exception = await act.Should().ThrowAsync<InvalidOperationException>();
        exception.Which.Message.Should().Contain("Failed to retrieve city data from external service");
        exception.Which.InnerException.Should().BeOfType<ExternalApiException>();
    }

    [Fact]
    public async Task SearchCitiesAsync_NotFoundError_ReturnsEmptyList()
    {
        // Arrange
        var request = TestData.ValidCitySearchRequest;
        _mockApiClient.GetGeocodingAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new ExternalApiException("Not found", 404));

        // Act
        var result = await _service.SearchCitiesAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchCitiesAsync_UnexpectedException_ThrowsInvalidOperationException()
    {
        // Arrange
        var request = TestData.ValidCitySearchRequest;
        _mockApiClient.GetGeocodingAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("Unexpected error"));

        // Act & Assert
        var act = async () => await _service.SearchCitiesAsync(request);
        var exception = await act.Should().ThrowAsync<InvalidOperationException>();
        exception.Which.Message.Should().Contain("An unexpected error occurred while searching for cities");
    }

    [Fact]
    public async Task SearchCitiesAsync_LogsSearchRequest()
    {
        // Arrange
        var request = TestData.ValidCitySearchRequest;
        _mockApiClient.GetGeocodingAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(TestData.ValidGeocodingResponse);

        // Act
        await _service.SearchCitiesAsync(request);

        // Assert
        _mockLogger.Received().LogInformation(
            Arg.Is<string>(s => s.Contains("Searching cities for query")),
            request.Q, request.Count, request.Language);
    }

    [Fact]
    public async Task SearchCitiesAsync_LogsSearchCompletion()
    {
        // Arrange
        var request = TestData.ValidCitySearchRequest;
        _mockApiClient.GetGeocodingAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(TestData.ValidGeocodingResponse);

        // Act
        await _service.SearchCitiesAsync(request);

        // Assert
        _mockLogger.Received().LogInformation(
            Arg.Is<string>(s => s.Contains("City search completed")),
            Arg.Any<object[]>());
    }

    #endregion

    #region GetWeatherAsync Tests

    [Fact]
    public async Task GetWeatherAsync_ValidRequest_ReturnsWeatherData()
    {
        // Arrange
        var request = TestData.ValidWeatherRequest;
        _mockApiClient.GetForecastAsync(request.Lat, request.Lon, request.Days, Arg.Any<CancellationToken>())
            .Returns(TestData.ValidWeatherResponse);

        // Mock reverse geocoding attempt (this will fail gracefully)
        _mockApiClient.GetGeocodingAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(TestData.EmptyGeocodingResponse);

        // Act
        var result = await _service.GetWeatherAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Location.Should().NotBeNull();
        result.Current.Should().NotBeNull();
        result.Daily.Should().NotBeEmpty();
        result.Source.Provider.Should().Be("open-meteo");
    }

    [Fact]
    public async Task GetWeatherAsync_NullRequest_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = async () => await _service.GetWeatherAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GetWeatherAsync_NullResponse_ThrowsInvalidOperationException()
    {
        // Arrange
        var request = TestData.ValidWeatherRequest;
        _mockApiClient.GetForecastAsync(Arg.Any<double>(), Arg.Any<double>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns((Models.External.OpenMeteoWeatherResponse?)null);

        // Act & Assert
        var act = async () => await _service.GetWeatherAsync(request);
        var exception = await act.Should().ThrowAsync<InvalidOperationException>();
        exception.Which.Message.Should().Contain("Received null response from weather service");
    }

    [Fact]
    public async Task GetWeatherAsync_ExternalApiException_ThrowsInvalidOperationException()
    {
        // Arrange
        var request = TestData.ValidWeatherRequest;
        _mockApiClient.GetForecastAsync(Arg.Any<double>(), Arg.Any<double>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new ExternalApiException("API Error", 500));

        // Act & Assert
        var act = async () => await _service.GetWeatherAsync(request);
        var exception = await act.Should().ThrowAsync<InvalidOperationException>();
        exception.Which.Message.Should().Contain("Failed to retrieve weather data from external service");
        exception.Which.InnerException.Should().BeOfType<ExternalApiException>();
    }

    [Fact]
    public async Task GetWeatherAsync_UnexpectedException_ThrowsInvalidOperationException()
    {
        // Arrange
        var request = TestData.ValidWeatherRequest;
        _mockApiClient.GetForecastAsync(Arg.Any<double>(), Arg.Any<double>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("Unexpected error"));

        // Act & Assert
        var act = async () => await _service.GetWeatherAsync(request);
        var exception = await act.Should().ThrowAsync<InvalidOperationException>();
        exception.Which.Message.Should().Contain("An unexpected error occurred while retrieving weather data");
    }

    [Fact]
    public async Task GetWeatherAsync_LogsWeatherRequest()
    {
        // Arrange
        var request = TestData.ValidWeatherRequest;
        _mockApiClient.GetForecastAsync(Arg.Any<double>(), Arg.Any<double>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(TestData.ValidWeatherResponse);

        // Act
        await _service.GetWeatherAsync(request);

        // Assert
        _mockLogger.Received().LogInformation(
            Arg.Is<string>(s => s.Contains("Getting weather for coordinates")),
            request.Lat, request.Lon, request.Days);
    }

    [Fact]
    public async Task GetWeatherAsync_LogsWeatherCompletion()
    {
        // Arrange
        var request = TestData.ValidWeatherRequest;
        _mockApiClient.GetForecastAsync(Arg.Any<double>(), Arg.Any<double>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(TestData.ValidWeatherResponse);

        // Act
        await _service.GetWeatherAsync(request);

        // Assert
        _mockLogger.Received().LogInformation(
            Arg.Is<string>(s => s.Contains("Weather forecast completed")),
            request.Lat, request.Lon);
    }

    [Fact]
    public async Task GetWeatherAsync_WithSuccessfulReverseGeocoding_UsesLocationNames()
    {
        // Arrange
        var request = TestData.ValidWeatherRequest;
        var geocodingResponse = new Models.External.OpenMeteoGeocodingResponse
        {
            Results = [TestData.LondonCityResult]
        };

        _mockApiClient.GetForecastAsync(request.Lat, request.Lon, request.Days, Arg.Any<CancellationToken>())
            .Returns(TestData.ValidWeatherResponse);
        
        // Set up reverse geocoding to succeed
        _mockApiClient.GetGeocodingAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(geocodingResponse);

        // Act
        var result = await _service.GetWeatherAsync(request);

        // Assert
        result.Location.Name.Should().Be("London");
        result.Location.Country.Should().Be("United Kingdom");
    }

    [Fact]
    public async Task GetWeatherAsync_WithFailedReverseGeocoding_UsesDefaultNames()
    {
        // Arrange
        var request = TestData.ValidWeatherRequest;
        _mockApiClient.GetForecastAsync(request.Lat, request.Lon, request.Days, Arg.Any<CancellationToken>())
            .Returns(TestData.ValidWeatherResponse);
        
        // Set up reverse geocoding to fail
        _mockApiClient.GetGeocodingAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("Reverse geocoding failed"));

        // Act
        var result = await _service.GetWeatherAsync(request);

        // Assert
        result.Location.Name.Should().Be("Unknown Location");
        result.Location.Country.Should().Be("Unknown");
        
        // Should log debug message about failed reverse geocoding
        _mockLogger.Received().LogDebug(
            Arg.Any<Exception>(),
            Arg.Is<string>(s => s.Contains("Failed to reverse geocode coordinates")),
            Arg.Any<object[]>());
    }

    #endregion

    #region Error Handling Tests

    [Theory]
    [InlineData(404, "no results found")]
    [InlineData(404, "")]
    public async Task SearchCitiesAsync_NotFoundErrors_ReturnsEmptyList(int statusCode, string responseContent)
    {
        // Arrange
        var request = TestData.ValidCitySearchRequest;
        _mockApiClient.GetGeocodingAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new ExternalApiException("Not found", statusCode, responseContent));

        // Act
        var result = await _service.SearchCitiesAsync(request);

        // Assert
        result.Should().BeEmpty();
        
        _mockLogger.Received().LogInformation(
            Arg.Is<string>(s => s.Contains("No cities found for query")),
            request.Q);
    }

    [Fact]
    public async Task SearchCitiesAsync_ErrorWithNoResultsContent_ReturnsEmptyList()
    {
        // Arrange
        var request = TestData.ValidCitySearchRequest;
        _mockApiClient.GetGeocodingAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new ExternalApiException("Error", 500, "no results"));

        // Act
        var result = await _service.SearchCitiesAsync(request);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region CancellationToken Tests

    [Fact]
    public async Task SearchCitiesAsync_WithCancellationToken_PassesToApiClient()
    {
        // Arrange
        var request = TestData.ValidCitySearchRequest;
        var cancellationToken = new CancellationToken();
        _mockApiClient.GetGeocodingAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<string>(), cancellationToken)
            .Returns(TestData.ValidGeocodingResponse);

        // Act
        await _service.SearchCitiesAsync(request, cancellationToken);

        // Assert
        await _mockApiClient.Received().GetGeocodingAsync(
            request.Q, request.Count, request.Language, cancellationToken);
    }

    [Fact]
    public async Task GetWeatherAsync_WithCancellationToken_PassesToApiClient()
    {
        // Arrange
        var request = TestData.ValidWeatherRequest;
        var cancellationToken = new CancellationToken();
        _mockApiClient.GetForecastAsync(Arg.Any<double>(), Arg.Any<double>(), Arg.Any<int>(), cancellationToken)
            .Returns(TestData.ValidWeatherResponse);

        // Act
        await _service.GetWeatherAsync(request, cancellationToken);

        // Assert
        await _mockApiClient.Received().GetForecastAsync(
            request.Lat, request.Lon, request.Days, cancellationToken);
    }

    #endregion
}
