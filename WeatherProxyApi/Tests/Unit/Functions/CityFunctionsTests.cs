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
/// Unit tests for CityFunctions
/// </summary>
public class CityFunctionsTests
{
    private readonly IWeatherService _mockWeatherService;
    private readonly IValidator<CitySearchRequest> _mockValidator;
    private readonly ILogger<CityFunctions> _mockLogger;
    private readonly CityFunctions _function;

    public CityFunctionsTests()
    {
        _mockWeatherService = Substitute.For<IWeatherService>();
        _mockValidator = Substitute.For<IValidator<CitySearchRequest>>();
        _mockLogger = Substitute.For<ILogger<CityFunctions>>();
        _function = new CityFunctions(_mockWeatherService, _mockValidator, _mockLogger);
    }

    [Fact]
    public async Task SearchCities_ValidRequest_Returns200WithCities()
    {
        // Arrange
        var httpRequest = CreateHttpRequest("q=London&count=5&language=en");
        var cities = new List<CityResult> { TestData.ExpectedLondonResult };
        
        _mockValidator.ValidateAsync(Arg.Any<CitySearchRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());
        
        _mockWeatherService.SearchCitiesAsync(Arg.Any<CitySearchRequest>(), Arg.Any<CancellationToken>())
            .Returns(cities);

        // Act
        var response = await _function.SearchCities(httpRequest, CancellationToken.None);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.Should().ContainKey("x-correlation-id");
        
        // Verify service was called
        await _mockWeatherService.Received().SearchCitiesAsync(
            Arg.Is<CitySearchRequest>(r => r.Q == "London" && r.Count == 5 && r.Language == "en"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchCities_EmptyQuery_Returns400WithValidationErrors()
    {
        // Arrange
        var httpRequest = CreateHttpRequest("q=&count=5&language=en");
        var validationFailure = new ValidationFailure("Q", "Search query is required");
        var validationResult = new ValidationResult(new[] { validationFailure });
        
        _mockValidator.ValidateAsync(Arg.Any<CitySearchRequest>(), Arg.Any<CancellationToken>())
            .Returns(validationResult);

        // Act
        var response = await _function.SearchCities(httpRequest, CancellationToken.None);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Headers.Should().ContainKey("Content-Type")
            .WhoseValue.Should().Contain("application/problem+json");
    }

    [Fact]
    public async Task SearchCities_InvalidCount_Returns400WithValidationErrors()
    {
        // Arrange
        var httpRequest = CreateHttpRequest("q=London&count=0&language=en");
        var validationFailure = new ValidationFailure("Count", "Count must be greater than 0");
        var validationResult = new ValidationResult(new[] { validationFailure });
        
        _mockValidator.ValidateAsync(Arg.Any<CitySearchRequest>(), Arg.Any<CancellationToken>())
            .Returns(validationResult);

        // Act
        var response = await _function.SearchCities(httpRequest, CancellationToken.None);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        
        // Verify logger was called for validation failure
        _mockLogger.Received().LogWarning(
            Arg.Is<string>(s => s.Contains("City search validation failed")),
            Arg.Any<object[]>());
    }

    [Fact]
    public async Task SearchCities_NoResults_Returns404WithProblemDetails()
    {
        // Arrange
        var httpRequest = CreateHttpRequest("q=NonexistentCity&count=5&language=en");
        
        _mockValidator.ValidateAsync(Arg.Any<CitySearchRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());
        
        _mockWeatherService.SearchCitiesAsync(Arg.Any<CitySearchRequest>(), Arg.Any<CancellationToken>())
            .Returns(new List<CityResult>());

        // Act
        var response = await _function.SearchCities(httpRequest, CancellationToken.None);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Headers.Should().ContainKey("Content-Type")
            .WhoseValue.Should().Contain("application/problem+json");
        
        // Verify logger was called for no results
        _mockLogger.Received().LogWarning(
            Arg.Is<string>(s => s.Contains("City search returned no results")),
            Arg.Any<object[]>());
    }

    [Fact]
    public async Task SearchCities_ServiceException_Returns502WithProblemDetails()
    {
        // Arrange
        var httpRequest = CreateHttpRequest("q=London&count=5&language=en");
        
        _mockValidator.ValidateAsync(Arg.Any<CitySearchRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());
        
        _mockWeatherService.SearchCitiesAsync(Arg.Any<CitySearchRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("External service error"));

        // Act
        var response = await _function.SearchCities(httpRequest, CancellationToken.None);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadGateway);
        response.Headers.Should().ContainKey("Content-Type")
            .WhoseValue.Should().Contain("application/problem+json");
        
        // Verify logger was called for service error
        _mockLogger.Received().LogError(
            Arg.Any<Exception>(),
            Arg.Is<string>(s => s.Contains("Service error during city search")),
            Arg.Any<object[]>());
    }

    [Fact]
    public async Task SearchCities_UnexpectedException_Returns500WithProblemDetails()
    {
        // Arrange
        var httpRequest = CreateHttpRequest("q=London&count=5&language=en");
        
        _mockValidator.ValidateAsync(Arg.Any<CitySearchRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());
        
        _mockWeatherService.SearchCitiesAsync(Arg.Any<CitySearchRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("Unexpected error"));

        // Act
        var response = await _function.SearchCities(httpRequest, CancellationToken.None);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        
        // Verify logger was called for unexpected error
        _mockLogger.Received().LogError(
            Arg.Any<Exception>(),
            Arg.Is<string>(s => s.Contains("Unexpected error during city search")),
            Arg.Any<object[]>());
    }

    [Fact]
    public async Task SearchCities_WithCorrelationId_UsesProvidedId()
    {
        // Arrange
        var correlationId = Guid.NewGuid().ToString();
        var httpRequest = CreateHttpRequest("q=London&count=5&language=en", correlationId);
        
        _mockValidator.ValidateAsync(Arg.Any<CitySearchRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());
        
        _mockWeatherService.SearchCitiesAsync(Arg.Any<CitySearchRequest>(), Arg.Any<CancellationToken>())
            .Returns(new List<CityResult> { TestData.ExpectedLondonResult });

        // Act
        var response = await _function.SearchCities(httpRequest, CancellationToken.None);

        // Assert
        response.Headers.Should().ContainKey("x-correlation-id")
            .WhoseValue.Should().Contain(correlationId);
    }

    [Fact]
    public async Task SearchCities_WithoutCorrelationId_GeneratesNewId()
    {
        // Arrange
        var httpRequest = CreateHttpRequest("q=London&count=5&language=en");
        
        _mockValidator.ValidateAsync(Arg.Any<CitySearchRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());
        
        _mockWeatherService.SearchCitiesAsync(Arg.Any<CitySearchRequest>(), Arg.Any<CancellationToken>())
            .Returns(new List<CityResult> { TestData.ExpectedLondonResult });

        // Act
        var response = await _function.SearchCities(httpRequest, CancellationToken.None);

        // Assert
        response.Headers.Should().ContainKey("x-correlation-id");
        var correlationId = response.Headers["x-correlation-id"].First();
        Guid.TryParse(correlationId, out _).Should().BeTrue("Generated correlation ID should be a valid GUID");
    }

    [Fact]
    public async Task SearchCities_DefaultParameters_UsesDefaults()
    {
        // Arrange
        var httpRequest = CreateHttpRequest("q=London"); // Only required parameter
        
        _mockValidator.ValidateAsync(Arg.Any<CitySearchRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());
        
        _mockWeatherService.SearchCitiesAsync(Arg.Any<CitySearchRequest>(), Arg.Any<CancellationToken>())
            .Returns(new List<CityResult> { TestData.ExpectedLondonResult });

        // Act
        await _function.SearchCities(httpRequest, CancellationToken.None);

        // Assert
        await _mockWeatherService.Received().SearchCitiesAsync(
            Arg.Is<CitySearchRequest>(r => r.Q == "London" && r.Count == 5 && r.Language == "en"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SearchCities_LogsRequestAndCompletion()
    {
        // Arrange
        var httpRequest = CreateHttpRequest("q=London&count=5&language=en");
        
        _mockValidator.ValidateAsync(Arg.Any<CitySearchRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());
        
        _mockWeatherService.SearchCitiesAsync(Arg.Any<CitySearchRequest>(), Arg.Any<CancellationToken>())
            .Returns(new List<CityResult> { TestData.ExpectedLondonResult });

        // Act
        await _function.SearchCities(httpRequest, CancellationToken.None);

        // Assert
        // Verify request logging
        _mockLogger.Received().LogInformation(
            Arg.Is<string>(s => s.Contains("City search requested")),
            "London", 5, "en", Arg.Any<string>());
        
        // Verify completion logging
        _mockLogger.Received().LogInformation(
            Arg.Is<string>(s => s.Contains("City search completed")),
            "London", 1, Arg.Any<long>(), Arg.Any<string>());
    }

    private static HttpRequestData CreateHttpRequest(string queryString, string? correlationId = null)
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddScoped<ILoggerFactory, LoggerFactory>();
        var serviceProvider = serviceCollection.BuildServiceProvider();

        var context = Substitute.For<FunctionContext>();
        context.InstanceServices.Returns(serviceProvider);

        var request = Substitute.For<HttpRequestData>(context);
        request.Url.Returns(new Uri($"https://localhost/api/cities/search?{queryString}"));
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
