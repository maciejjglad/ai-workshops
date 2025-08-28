using System.Diagnostics;
using System.Net;
using FluentValidation;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Extensions.OpenApi.Extensions;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using WeatherProxyApi.Models.Requests;
using WeatherProxyApi.Models.Responses;
using WeatherProxyApi.Models.Errors;
using WeatherProxyApi.Services;

namespace WeatherProxyApi.Functions;

public class WeatherFunctions
{
    private readonly IWeatherService _weatherService;
    private readonly IValidator<WeatherRequest> _validator;
    private readonly ILogger<WeatherFunctions> _logger;

    public WeatherFunctions(
        IWeatherService weatherService,
        IValidator<WeatherRequest> validator,
        ILogger<WeatherFunctions> logger)
    {
        _weatherService = weatherService;
        _validator = validator;
        _logger = logger;
    }

    [Function("GetWeather")]
    [OpenApiOperation(operationId: "GetWeather", tags: new[] { "Weather" }, Summary = "Get weather forecast", Description = "Get weather forecast for specified coordinates")]
    [OpenApiParameter(name: "lat", In = ParameterLocation.Query, Required = true, Type = typeof(double), Description = "Latitude coordinate (-90 to 90)")]
    [OpenApiParameter(name: "lon", In = ParameterLocation.Query, Required = true, Type = typeof(double), Description = "Longitude coordinate (-180 to 180)")]
    [OpenApiParameter(name: "days", In = ParameterLocation.Query, Required = false, Type = typeof(int), Description = "Number of forecast days (default: 5, max: 16)")]
    [OpenApiParameter(name: "cityName", In = ParameterLocation.Query, Required = false, Type = typeof(string), Description = "Optional city name for better location display")]
    [OpenApiParameter(name: "countryName", In = ParameterLocation.Query, Required = false, Type = typeof(string), Description = "Optional country name for better location display")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(WeatherResponse), Description = "Successfully retrieved weather forecast")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/problem+json", bodyType: typeof(ApiProblemDetails), Description = "Invalid coordinates or parameters")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadGateway, contentType: "application/problem+json", bodyType: typeof(ApiProblemDetails), Description = "Weather service unavailable")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.InternalServerError, contentType: "application/problem+json", bodyType: typeof(ApiProblemDetails), Description = "Internal server error")]
    public async Task<HttpResponseData> GetWeather(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "api/weather")]
        HttpRequestData req,
        CancellationToken cancellationToken)
    {
        var correlationId = req.Headers.TryGetValues("x-correlation-id", out var values) ? values.FirstOrDefault() : null
            ?? Guid.NewGuid().ToString();

        using var activity = Activity.Current?.Source.StartActivity("GetWeather");
        activity?.SetTag("correlation-id", correlationId);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Extract request parameters
            var queryCollection = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
            var request = new WeatherRequest
            {
                Lat = double.TryParse(queryCollection["lat"], out var lat) ? lat : 0,
                Lon = double.TryParse(queryCollection["lon"], out var lon) ? lon : 0,
                Days = int.TryParse(queryCollection["days"], out var days) ? days : 5,
                CityName = queryCollection["cityName"],
                CountryName = queryCollection["countryName"]
            };

            _logger.LogInformation("Weather forecast requested {Latitude} {Longitude} {Days} {CorrelationId}",
                request.Lat, request.Lon, request.Days, correlationId);

            // Validate request
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Weather request validation failed for coordinates ({Latitude}, {Longitude}) {CorrelationId}",
                    request.Lat, request.Lon, correlationId);

                return await CreateProblemResponse(req, HttpStatusCode.BadRequest, "Validation Failed",
                    "One or more validation errors occurred.", correlationId,
                    validationResult.Errors.ToDictionary(e => e.PropertyName, e => new[] { e.ErrorMessage }));
            }

            // Call service
            var weather = await _weatherService.GetWeatherAsync(request, cancellationToken);

            stopwatch.Stop();
            _logger.LogInformation("Weather forecast completed {Latitude} {Longitude} {Duration}ms {CorrelationId}",
                request.Lat, request.Lon, stopwatch.ElapsedMilliseconds, correlationId);

            // Create successful response
            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("x-correlation-id", correlationId);
            await response.WriteAsJsonAsync(weather);

            return response;
        }
        catch (InvalidOperationException ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Service error during weather forecast {CorrelationId}", correlationId);

            return await CreateProblemResponse(req, HttpStatusCode.BadGateway, "External Service Error",
                "The weather data provider is currently unavailable. Please try again later.", correlationId,
                context: new Dictionary<string, object>
                {
                    ["upstreamService"] = "open-meteo.com",
                    ["upstreamError"] = ex.Message,
                    ["retryAfter"] = DateTime.UtcNow.AddMinutes(5).ToString("O")
                });
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Unexpected error during weather forecast {CorrelationId}", correlationId);

            return await CreateProblemResponse(req, HttpStatusCode.InternalServerError, "Internal Server Error",
                "An unexpected error occurred while processing your request.", correlationId);
        }
    }

    private static async Task<HttpResponseData> CreateProblemResponse(
        HttpRequestData req,
        HttpStatusCode statusCode,
        string title,
        string detail,
        string correlationId,
        Dictionary<string, string[]>? errors = null,
        Dictionary<string, object>? context = null)
    {
        var problemDetails = new ApiProblemDetails
        {
            Type = statusCode switch
            {
                HttpStatusCode.BadRequest => "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                HttpStatusCode.BadGateway => "https://example.com/problems/upstream-error",
                _ => "https://tools.ietf.org/html/rfc7231#section-6.6.1"
            },
            Title = title,
            Status = (int)statusCode,
            Detail = detail,
            Instance = req.Url.AbsolutePath,
            TraceId = Activity.Current?.Id,
            CorrelationId = correlationId,
            Timestamp = DateTime.UtcNow,
            Context = context
        };

        if (errors != null)
        {
            problemDetails.Extensions["errors"] = errors;
        }

        var response = req.CreateResponse(statusCode);
        response.Headers.Add("x-correlation-id", correlationId);
        response.Headers.Add("Content-Type", "application/problem+json");
        await response.WriteAsJsonAsync(problemDetails);

        return response;
    }
}
