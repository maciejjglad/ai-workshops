using System.Net;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using WeatherProxyApi.Functions;
using WeatherProxyApi.Models.Requests;
using WeatherProxyApi.Models.Responses;
using WeatherProxyApi.Services;
using WeatherProxyApi.Tests.TestFixtures;
using Xunit;

namespace WeatherProxyApi.Tests.Unit.Functions;

/// <summary>
/// Unit tests for WeatherFunctions
/// </summary>
public class WeatherFunctionsTests
{
    private readonly IWeatherService _mockWeatherService;
    private readonly IValidator<WeatherRequest> _mockValidator;
    private readonly ILogger<WeatherFunctions> _mockLogger;
    private readonly WeatherFunctions _function;

    public WeatherFunctionsTests()
    {
        _mockWeatherService = Substitute.For<IWeatherService>();
        _mockValidator = Substitute.For<IValidator<WeatherRequest>>();
        _mockLogger = Substitute.For<ILogger<WeatherFunctions>>();
        _function = new WeatherFunctions(_mockWeatherService, _mockValidator, _mockLogger);
    }

    [Fact]
    public async Task GetWeather_ValidRequest_Returns200WithWeatherData()
    {
        // Arrange
        var httpRequest = CreateHttpRequest("lat=51.5074&lon=-0.1278&days=5");
        var weatherResponse = CreateMockWeatherResponse();
        
        _mockValidator.ValidateAsync(Arg.Any<WeatherRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());
        
        _mockWeatherService.GetWeatherAsync(Arg.Any<WeatherRequest>(), Arg.Any<CancellationToken>())
            .Returns(weatherResponse);

        // Act
        var response = await _function.GetWeather(httpRequest, CancellationToken.None);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.Should().ContainKey("x-correlation-id");
        
        // Verify service was called
        await _mockWeatherService.Received().GetWeatherAsync(
            Arg.Is<WeatherRequest>(r => r.Lat == 51.5074 && r.Lon == -0.1278 && r.Days == 5),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetWeather_InvalidLatitude_Returns400WithValidationErrors()
    {
        // Arrange
        var httpRequest = CreateHttpRequest("lat=91.0&lon=0.0&days=5");
        var validationFailure = new ValidationFailure("Lat", "Latitude must be between -90 and 90");
        var validationResult = new ValidationResult(new[] { validationFailure });
        
        _mockValidator.ValidateAsync(Arg.Any<WeatherRequest>(), Arg.Any<CancellationToken>())
            .Returns(validationResult);

        // Act
        var response = await _function.GetWeather(httpRequest, CancellationToken.None);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Headers.Should().ContainKey("Content-Type")
            .WhoseValue.Should().Contain("application/problem+json");
    }

    [Fact]
    public async Task GetWeather_InvalidLongitude_Returns400WithValidationErrors()
    {
        // Arrange
        var httpRequest = CreateHttpRequest("lat=0.0&lon=181.0&days=5");
        var validationFailure = new ValidationFailure("Lon", "Longitude must be between -180 and 180");
        var validationResult = new ValidationResult(new[] { validationFailure });
        
        _mockValidator.ValidateAsync(Arg.Any<WeatherRequest>(), Arg.Any<CancellationToken>())
            .Returns(validationResult);

        // Act
        var response = await _function.GetWeather(httpRequest, CancellationToken.None);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        
        // Verify logger was called for validation failure
        _mockLogger.Received().LogWarning(
            Arg.Is<string>(s => s.Contains("Weather request validation failed")),
            Arg.Any<object[]>());
    }

    [Fact]
    public async Task GetWeather_InvalidDays_Returns400WithValidationErrors()
    {
        // Arrange
        var httpRequest = CreateHttpRequest("lat=51.5074&lon=-0.1278&days=0");
        var validationFailure = new ValidationFailure("Days", "Days must be greater than 0");
        var validationResult = new ValidationResult(new[] { validationFailure });
        
        _mockValidator.ValidateAsync(Arg.Any<WeatherRequest>(), Arg.Any<CancellationToken>())
            .Returns(validationResult);

        // Act
        var response = await _function.GetWeather(httpRequest, CancellationToken.None);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetWeather_ServiceException_Returns502WithProblemDetails()
    {
        // Arrange
        var httpRequest = CreateHttpRequest("lat=51.5074&lon=-0.1278&days=5");
        
        _mockValidator.ValidateAsync(Arg.Any<WeatherRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());
        
        _mockWeatherService.GetWeatherAsync(Arg.Any<WeatherRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("External service error"));

        // Act
        var response = await _function.GetWeather(httpRequest, CancellationToken.None);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadGateway);
        response.Headers.Should().ContainKey("Content-Type")
            .WhoseValue.Should().Contain("application/problem+json");
        
        // Verify logger was called for service error
        _mockLogger.Received().LogError(
            Arg.Any<Exception>(),
            Arg.Is<string>(s => s.Contains("Service error during weather forecast")),
            Arg.Any<object[]>());
    }

    [Fact]
    public async Task GetWeather_UnexpectedException_Returns500WithProblemDetails()
    {
        // Arrange
        var httpRequest = CreateHttpRequest("lat=51.5074&lon=-0.1278&days=5");
        
        _mockValidator.ValidateAsync(Arg.Any<WeatherRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());
        
        _mockWeatherService.GetWeatherAsync(Arg.Any<WeatherRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("Unexpected error"));

        // Act
        var response = await _function.GetWeather(httpRequest, CancellationToken.None);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        
        // Verify logger was called for unexpected error
        _mockLogger.Received().LogError(
            Arg.Any<Exception>(),
            Arg.Is<string>(s => s.Contains("Unexpected error during weather forecast")),
            Arg.Any<object[]>());
    }

    [Fact]
    public async Task GetWeather_WithCorrelationId_UsesProvidedId()
    {
        // Arrange
        var correlationId = Guid.NewGuid().ToString();
        var httpRequest = CreateHttpRequest("lat=51.5074&lon=-0.1278&days=5", correlationId);
        
        _mockValidator.ValidateAsync(Arg.Any<WeatherRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());
        
        _mockWeatherService.GetWeatherAsync(Arg.Any<WeatherRequest>(), Arg.Any<CancellationToken>())
            .Returns(CreateMockWeatherResponse());

        // Act
        var response = await _function.GetWeather(httpRequest, CancellationToken.None);

        // Assert
        response.Headers.Should().ContainKey("x-correlation-id")
            .WhoseValue.Should().Contain(correlationId);
    }

    [Fact]
    public async Task GetWeather_WithoutCorrelationId_GeneratesNewId()
    {
        // Arrange
        var httpRequest = CreateHttpRequest("lat=51.5074&lon=-0.1278&days=5");
        
        _mockValidator.ValidateAsync(Arg.Any<WeatherRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());
        
        _mockWeatherService.GetWeatherAsync(Arg.Any<WeatherRequest>(), Arg.Any<CancellationToken>())
            .Returns(CreateMockWeatherResponse());

        // Act
        var response = await _function.GetWeather(httpRequest, CancellationToken.None);

        // Assert
        response.Headers.Should().ContainKey("x-correlation-id");
        var correlationId = response.Headers["x-correlation-id"].First();
        Guid.TryParse(correlationId, out _).Should().BeTrue("Generated correlation ID should be a valid GUID");
    }

    [Fact]
    public async Task GetWeather_DefaultDays_UsesDefault()
    {
        // Arrange
        var httpRequest = CreateHttpRequest("lat=51.5074&lon=-0.1278"); // No days parameter
        
        _mockValidator.ValidateAsync(Arg.Any<WeatherRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());
        
        _mockWeatherService.GetWeatherAsync(Arg.Any<WeatherRequest>(), Arg.Any<CancellationToken>())
            .Returns(CreateMockWeatherResponse());

        // Act
        await _function.GetWeather(httpRequest, CancellationToken.None);

        // Assert
        await _mockWeatherService.Received().GetWeatherAsync(
            Arg.Is<WeatherRequest>(r => r.Lat == 51.5074 && r.Lon == -0.1278 && r.Days == 5),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetWeather_InvalidNumberFormat_UsesDefault()
    {
        // Arrange
        var httpRequest = CreateHttpRequest("lat=invalid&lon=-0.1278&days=abc");
        
        _mockValidator.ValidateAsync(Arg.Any<WeatherRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());
        
        _mockWeatherService.GetWeatherAsync(Arg.Any<WeatherRequest>(), Arg.Any<CancellationToken>())
            .Returns(CreateMockWeatherResponse());

        // Act
        await _function.GetWeather(httpRequest, CancellationToken.None);

        // Assert
        await _mockWeatherService.Received().GetWeatherAsync(
            Arg.Is<WeatherRequest>(r => r.Lat == 0.0 && r.Lon == -0.1278 && r.Days == 5),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetWeather_LogsRequestAndCompletion()
    {
        // Arrange
        var httpRequest = CreateHttpRequest("lat=51.5074&lon=-0.1278&days=5");
        
        _mockValidator.ValidateAsync(Arg.Any<WeatherRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());
        
        _mockWeatherService.GetWeatherAsync(Arg.Any<WeatherRequest>(), Arg.Any<CancellationToken>())
            .Returns(CreateMockWeatherResponse());

        // Act
        await _function.GetWeather(httpRequest, CancellationToken.None);

        // Assert
        // Verify request logging
        _mockLogger.Received().LogInformation(
            Arg.Is<string>(s => s.Contains("Weather forecast requested")),
            51.5074, -0.1278, 5, Arg.Any<string>());
        
        // Verify completion logging
        _mockLogger.Received().LogInformation(
            Arg.Is<string>(s => s.Contains("Weather forecast completed")),
            51.5074, -0.1278, Arg.Any<long>(), Arg.Any<string>());
    }

    [Fact]
    public async Task GetWeather_MultipleValidationErrors_Returns400WithAllErrors()
    {
        // Arrange
        var httpRequest = CreateHttpRequest("lat=91.0&lon=181.0&days=0");
        var validationFailures = new[]
        {
            new ValidationFailure("Lat", "Latitude must be between -90 and 90"),
            new ValidationFailure("Lon", "Longitude must be between -180 and 180"),
            new ValidationFailure("Days", "Days must be greater than 0")
        };
        var validationResult = new ValidationResult(validationFailures);
        
        _mockValidator.ValidateAsync(Arg.Any<WeatherRequest>(), Arg.Any<CancellationToken>())
            .Returns(validationResult);

        // Act
        var response = await _function.GetWeather(httpRequest, CancellationToken.None);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Headers.Should().ContainKey("Content-Type")
            .WhoseValue.Should().Contain("application/problem+json");
    }

    [Theory]
    [InlineData("lat=90.0&lon=180.0&days=1")] // Maximum valid values
    [InlineData("lat=-90.0&lon=-180.0&days=7")] // Minimum valid values
    [InlineData("lat=0.0&lon=0.0&days=3")] // Zero coordinates
    public async Task GetWeather_EdgeCaseValidCoordinates_ReturnsSuccess(string queryString)
    {
        // Arrange
        var httpRequest = CreateHttpRequest(queryString);
        
        _mockValidator.ValidateAsync(Arg.Any<WeatherRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());
        
        _mockWeatherService.GetWeatherAsync(Arg.Any<WeatherRequest>(), Arg.Any<CancellationToken>())
            .Returns(CreateMockWeatherResponse());

        // Act
        var response = await _function.GetWeather(httpRequest, CancellationToken.None);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private static WeatherResponse CreateMockWeatherResponse()
    {
        return new WeatherResponse
        {
            Location = new LocationData
            {
                Name = "London",
                Country = "United Kingdom",
                Latitude = 51.5074,
                Longitude = -0.1278,
                Timezone = "Europe/London"
            },
            Current = TestData.ExpectedCurrentWeather,
            Daily = new List<DailyWeatherData> { TestData.ExpectedFirstDayWeather },
            Source = new SourceInfo
            {
                Provider = "open-meteo",
                Model = "best_match"
            }
        };
    }

    private static HttpRequestData CreateHttpRequest(string queryString, string? correlationId = null)
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddScoped<ILoggerFactory, LoggerFactory>();
        var serviceProvider = serviceCollection.BuildServiceProvider();

        var context = Substitute.For<FunctionContext>();
        context.InstanceServices.Returns(serviceProvider);

        var request = Substitute.For<HttpRequestData>(context);
        request.Url.Returns(new Uri($"https://localhost/api/weather?{queryString}"));
        request.Query.Returns(System.Web.HttpUtility.ParseQueryString(queryString));

        var headers = new HttpHeadersCollection();
        if (correlationId != null)
        {
            headers.Add("x-correlation-id", correlationId);
        }
        
        request.Headers.Returns(headers);
        request.CreateResponse().Returns(callInfo =>
        {
            var response = Substitute.For<HttpResponseData>(context);
            response.Headers.Returns(new HttpHeadersCollection());
            response.StatusCode.Returns(HttpStatusCode.OK);
            return response;
        });

        return request;
    }
}
