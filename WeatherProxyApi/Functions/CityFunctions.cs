using System.Diagnostics;
using System.Net;
using FluentValidation;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using WeatherProxyApi.Models.Requests;
using WeatherProxyApi.Models.Responses;
using WeatherProxyApi.Models.Errors;
using WeatherProxyApi.Services;

namespace WeatherProxyApi.Functions;

public class CityFunctions
{
    private readonly IWeatherService _weatherService;
    private readonly IValidator<CitySearchRequest> _validator;
    private readonly ILogger<CityFunctions> _logger;

    public CityFunctions(
        IWeatherService weatherService,
        IValidator<CitySearchRequest> validator,
        ILogger<CityFunctions> logger)
    {
        _weatherService = weatherService;
        _validator = validator;
        _logger = logger;
    }

    [Function("SearchCities")]
    public async Task<HttpResponseData> SearchCities(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "api/cities/search")]
        HttpRequestData req,
        CancellationToken cancellationToken)
    {
        var correlationId = req.Headers.GetValues("x-correlation-id").FirstOrDefault() 
            ?? Guid.NewGuid().ToString();

        using var activity = Activity.Current?.Source.StartActivity("SearchCities");
        activity?.SetTag("correlation-id", correlationId);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Extract request parameters
            var queryCollection = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
            var request = new CitySearchRequest
            {
                Q = queryCollection["q"] ?? string.Empty,
                Count = int.TryParse(queryCollection["count"], out var count) ? count : 5,
                Language = queryCollection["language"] ?? "en"
            };

            _logger.LogInformation("City search requested {SearchQuery} {Count} {Language} {CorrelationId}",
                request.Q, request.Count, request.Language, correlationId);

            // Validate request
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("City search validation failed for query '{SearchQuery}' {CorrelationId}",
                    request.Q, correlationId);

                return await CreateProblemResponse(req, HttpStatusCode.BadRequest, "Validation Failed",
                    "One or more validation errors occurred.", correlationId,
                    validationResult.Errors.ToDictionary(e => e.PropertyName, e => new[] { e.ErrorMessage }));
            }

            // Call service
            var cities = await _weatherService.SearchCitiesAsync(request, cancellationToken);

            if (!cities.Any())
            {
                _logger.LogWarning("City search returned no results {SearchQuery} {CorrelationId}",
                    request.Q, correlationId);

                return await CreateProblemResponse(req, HttpStatusCode.NotFound, "City Not Found",
                    $"No cities found matching the search criteria '{request.Q}'.", correlationId,
                    context: new Dictionary<string, object>
                    {
                        ["searchQuery"] = request.Q,
                        ["searchParameters"] = new { request.Count, request.Language }
                    });
            }

            stopwatch.Stop();
            _logger.LogInformation("City search completed {SearchQuery} {ResultCount} {Duration}ms {CorrelationId}",
                request.Q, cities.Count, stopwatch.ElapsedMilliseconds, correlationId);

            // Create successful response
            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("x-correlation-id", correlationId);
            await response.WriteAsJsonAsync(new CitySearchResponse { Cities = cities });

            return response;
        }
        catch (InvalidOperationException ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Service error during city search {CorrelationId}", correlationId);

            return await CreateProblemResponse(req, HttpStatusCode.BadGateway, "External Service Error",
                "The city search service is currently unavailable. Please try again later.", correlationId,
                context: new Dictionary<string, object>
                {
                    ["upstreamService"] = "open-meteo.com",
                    ["upstreamError"] = ex.Message
                });
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Unexpected error during city search {CorrelationId}", correlationId);

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
                HttpStatusCode.NotFound => "https://example.com/problems/city-not-found",
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
