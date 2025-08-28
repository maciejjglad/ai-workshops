using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using WeatherProxyApi.Models.External;
using WeatherProxyApi.Services;
using WeatherProxyApi.Services.Exceptions;
using WeatherProxyApi.Tests.TestFixtures;
using Xunit;

namespace WeatherProxyApi.Tests.Unit.Services;

/// <summary>
/// Unit tests for WeatherApiClient
/// </summary>
public class WeatherApiClientTests : IDisposable
{
    private readonly MockHttpMessageHandler _mockHandler;
    private readonly HttpClient _geocodingClient;
    private readonly HttpClient _forecastClient;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<WeatherApiClient> _logger;
    private readonly WeatherApiClient _client;

    public WeatherApiClientTests()
    {
        _mockHandler = new MockHttpMessageHandler();
        _geocodingClient = new HttpClient(_mockHandler) { BaseAddress = new Uri("https://geocoding-api.open-meteo.com/") };
        _forecastClient = new HttpClient(_mockHandler) { BaseAddress = new Uri("https://api.open-meteo.com/") };
        
        _httpClientFactory = Substitute.For<IHttpClientFactory>();
        _httpClientFactory.CreateClient("OpenMeteoGeocoding").Returns(_geocodingClient);
        _httpClientFactory.CreateClient("OpenMeteoForecast").Returns(_forecastClient);
        
        _logger = Substitute.For<ILogger<WeatherApiClient>>();
        _client = new WeatherApiClient(_httpClientFactory, _logger);
    }

    public void Dispose()
    {
        _geocodingClient.Dispose();
        _forecastClient.Dispose();
        _mockHandler.Reset();
    }

    #region Geocoding Tests

    [Fact]
    public async Task GetGeocodingAsync_ValidRequest_ReturnsResponse()
    {
        // Arrange
        var responseJson = JsonSerializer.Serialize(TestData.ValidGeocodingResponse, Models.WeatherApiJsonContext.Default.OpenMeteoGeocodingResponse);
        _mockHandler.AddSuccessResponse(responseJson);

        // Act
        var result = await _client.GetGeocodingAsync("London", 5, "en");

        // Assert
        result.Should().NotBeNull();
        result!.Results.Should().HaveCount(2);
        result.Results!.First().Name.Should().Be("London");
        
        _mockHandler.VerifyRequestCount(1);
        _mockHandler.VerifyRequestUrl(0, "v1/search?name=London&count=5&language=en&format=json");
    }

    [Fact]
    public async Task GetGeocodingAsync_EmptyResponse_ReturnsEmptyResults()
    {
        // Arrange
        _mockHandler.AddSuccessResponse("");

        // Act
        var result = await _client.GetGeocodingAsync("NonexistentCity", 5, "en");

        // Assert
        result.Should().NotBeNull();
        result!.Results.Should().BeEmpty();
    }

    [Fact]
    public async Task GetGeocodingAsync_EmptyName_ThrowsArgumentException()
    {
        // Act & Assert
        var act = async () => await _client.GetGeocodingAsync("", 5, "en");
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GetGeocodingAsync_ZeroCount_ThrowsArgumentOutOfRangeException()
    {
        // Act & Assert
        var act = async () => await _client.GetGeocodingAsync("London", 0, "en");
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task GetGeocodingAsync_SpecialCharacters_EscapesCorrectly()
    {
        // Arrange
        var responseJson = JsonSerializer.Serialize(TestData.EmptyGeocodingResponse, Models.WeatherApiJsonContext.Default.OpenMeteoGeocodingResponse);
        _mockHandler.AddSuccessResponse(responseJson);

        // Act
        await _client.GetGeocodingAsync("São Paulo", 3, "pt");

        // Assert
        _mockHandler.VerifyRequestUrl(0, "v1/search?name=S%C3%A3o%20Paulo&count=3&language=pt&format=json");
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.UnprocessableEntity)]
    public async Task GetGeocodingAsync_ClientErrors_ThrowsExternalApiException(HttpStatusCode statusCode)
    {
        // Arrange
        _mockHandler.AddErrorResponse(statusCode, "Client error");

        // Act & Assert
        var act = async () => await _client.GetGeocodingAsync("London", 5, "en");
        var exception = await act.Should().ThrowAsync<ExternalApiException>();
        exception.Which.HttpStatusCode.Should().Be((int)statusCode);
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.BadGateway)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task GetGeocodingAsync_ServerErrors_ThrowsExternalApiException(HttpStatusCode statusCode)
    {
        // Arrange
        _mockHandler.AddErrorResponse(statusCode, "Server error");

        // Act & Assert
        var act = async () => await _client.GetGeocodingAsync("London", 5, "en");
        var exception = await act.Should().ThrowAsync<ExternalApiException>();
        exception.Which.HttpStatusCode.Should().Be((int)statusCode);
    }

    [Fact]
    public async Task GetGeocodingAsync_MalformedJson_ThrowsExternalApiException()
    {
        // Arrange
        _mockHandler.AddSuccessResponse(TestData.MalformedJsonResponse);

        // Act & Assert
        var act = async () => await _client.GetGeocodingAsync("London", 5, "en");
        await act.Should().ThrowAsync<ExternalApiException>()
            .Where(e => e.Message.Contains("Invalid response format"));
    }

    #endregion

    #region Weather Forecast Tests

    [Fact]
    public async Task GetForecastAsync_ValidRequest_ReturnsResponse()
    {
        // Arrange
        var responseJson = JsonSerializer.Serialize(TestData.ValidWeatherResponse, Models.WeatherApiJsonContext.Default.OpenMeteoWeatherResponse);
        _mockHandler.AddSuccessResponse(responseJson);

        // Act
        var result = await _client.GetForecastAsync(51.5074, -0.1278, 5);

        // Assert
        result.Should().NotBeNull();
        result!.Latitude.Should().Be(51.5074);
        result.Longitude.Should().Be(-0.1278);
        result.Current.Should().NotBeNull();
        result.Daily.Should().NotBeNull();
        
        _mockHandler.VerifyRequestCount(1);
        _mockHandler.VerifyRequestUrl(0, "v1/forecast?latitude=51.507400&longitude=-0.127800");
    }

    [Theory]
    [InlineData(91.0, 0.0)] // Invalid latitude
    [InlineData(-91.0, 0.0)] // Invalid latitude
    [InlineData(0.0, 181.0)] // Invalid longitude
    [InlineData(0.0, -181.0)] // Invalid longitude
    public async Task GetForecastAsync_InvalidCoordinates_ThrowsArgumentOutOfRangeException(double lat, double lon)
    {
        // Act & Assert
        var act = async () => await _client.GetForecastAsync(lat, lon, 5);
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetForecastAsync_InvalidForecastDays_ThrowsArgumentOutOfRangeException(int days)
    {
        // Act & Assert
        var act = async () => await _client.GetForecastAsync(51.5074, -0.1278, days);
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task GetForecastAsync_EmptyResponse_ThrowsExternalApiException()
    {
        // Arrange
        _mockHandler.AddSuccessResponse("");

        // Act & Assert
        var act = async () => await _client.GetForecastAsync(51.5074, -0.1278, 5);
        await act.Should().ThrowAsync<ExternalApiException>()
            .Where(e => e.Message.Contains("Empty response"));
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.UnprocessableEntity)]
    public async Task GetForecastAsync_ClientErrors_ThrowsExternalApiException(HttpStatusCode statusCode)
    {
        // Arrange
        _mockHandler.AddErrorResponse(statusCode, "Invalid coordinates");

        // Act & Assert
        var act = async () => await _client.GetForecastAsync(51.5074, -0.1278, 5);
        var exception = await act.Should().ThrowAsync<ExternalApiException>();
        exception.Which.HttpStatusCode.Should().Be((int)statusCode);
    }

    [Fact]
    public async Task GetForecastAsync_MalformedJson_ThrowsExternalApiException()
    {
        // Arrange
        _mockHandler.AddSuccessResponse(TestData.MalformedJsonResponse);

        // Act & Assert
        var act = async () => await _client.GetForecastAsync(51.5074, -0.1278, 5);
        await act.Should().ThrowAsync<ExternalApiException>()
            .Where(e => e.Message.Contains("Invalid response format"));
    }

    #endregion

    #region URL Building Tests

    [Fact]
    public async Task GetGeocodingAsync_BuildsCorrectUrl()
    {
        // Arrange
        var responseJson = JsonSerializer.Serialize(TestData.EmptyGeocodingResponse, Models.WeatherApiJsonContext.Default.OpenMeteoGeocodingResponse);
        _mockHandler.AddSuccessResponse(responseJson);

        // Act
        await _client.GetGeocodingAsync("New York", 3, "en");

        // Assert
        _mockHandler.VerifyRequestUrl(0, "v1/search?name=New%20York&count=3&language=en&format=json");
    }

    [Fact]
    public async Task GetForecastAsync_BuildsCorrectUrl()
    {
        // Arrange
        var responseJson = JsonSerializer.Serialize(TestData.ValidWeatherResponse, Models.WeatherApiJsonContext.Default.OpenMeteoWeatherResponse);
        _mockHandler.AddSuccessResponse(responseJson);

        // Act
        await _client.GetForecastAsync(50.0647, 19.9450, 7);

        // Assert
        var request = _mockHandler.Requests.First();
        var url = request.RequestUri!.ToString();
        
        url.Should().Contain("latitude=50.064700");
        url.Should().Contain("longitude=19.945000");
        url.Should().Contain("forecast_days=7");
        url.Should().Contain("current=temperature_2m,wind_speed_10m,is_day,weather_code");
        url.Should().Contain("daily=weather_code,temperature_2m_max,temperature_2m_min,precipitation_probability_max,wind_speed_10m_max");
        url.Should().Contain("timezone=auto");
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task GetGeocodingAsync_HttpRequestException_ThrowsExternalApiException()
    {
        // Arrange
        _mockHandler.AddResponse(new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("Network error")
        });

        // Act & Assert
        var act = async () => await _client.GetGeocodingAsync("London", 5, "en");
        await act.Should().ThrowAsync<ExternalApiException>()
            .Where(e => e.Message.Contains("Failed to retrieve geocoding data"));
    }

    [Fact]
    public async Task GetForecastAsync_TaskCanceledException_ThrowsExternalApiException()
    {
        // Arrange
        var cancellationTokenSource = new CancellationTokenSource();
        var responseJson = JsonSerializer.Serialize(TestData.ValidWeatherResponse, Models.WeatherApiJsonContext.Default.OpenMeteoWeatherResponse);
        _mockHandler.AddSuccessResponse(responseJson);
        cancellationTokenSource.Cancel(); // Cancel immediately

        // Act & Assert
        var act = async () => await _client.GetForecastAsync(51.5074, -0.1278, 5, cancellationTokenSource.Token);
        await act.Should().ThrowAsync<ExternalApiException>()
            .Where(e => e.Message.Contains("timed out"));
    }

    #endregion

    #region Logging Tests

    [Fact]
    public async Task GetGeocodingAsync_LogsInformation()
    {
        // Arrange
        var responseJson = JsonSerializer.Serialize(TestData.ValidGeocodingResponse, Models.WeatherApiJsonContext.Default.OpenMeteoGeocodingResponse);
        _mockHandler.AddSuccessResponse(responseJson);

        // Act
        await _client.GetGeocodingAsync("London", 5, "en");

        // Assert
        _logger.Received().LogInformation(
            Arg.Is<string>(s => s.Contains("Calling Open-Meteo Geocoding API")),
            Arg.Any<object[]>());
    }

    [Fact]
    public async Task GetForecastAsync_LogsInformation()
    {
        // Arrange
        var responseJson = JsonSerializer.Serialize(TestData.ValidWeatherResponse, Models.WeatherApiJsonContext.Default.OpenMeteoWeatherResponse);
        _mockHandler.AddSuccessResponse(responseJson);

        // Act
        await _client.GetForecastAsync(51.5074, -0.1278, 5);

        // Assert
        _logger.Received().LogInformation(
            Arg.Is<string>(s => s.Contains("Calling Open-Meteo Forecast API")),
            Arg.Any<object[]>());
    }

    #endregion
}
