# Backend Endpoint Implementation Plan
**Weather Proxy API - .NET 9 Azure Functions (Isolated Worker)**

## Endpoint Contracts

### 1. City Search Endpoint
```
GET /api/cities/search
```

**Query Parameters:**
- `q` (required): City name to search (string, 2-100 chars)
- `count` (optional): Max results to return (int, 1-10, default: 5)
- `language` (optional): Language code (string, default: "en")

**Request Validation Rules:**
```csharp
// CitySearchRequestValidator
RuleFor(x => x.Q)
    .NotEmpty().WithMessage("Search query is required")
    .MinimumLength(2).WithMessage("Search query must be at least 2 characters")
    .MaximumLength(100).WithMessage("Search query cannot exceed 100 characters")
    .Matches(@"^[\p{L}\p{N}\s\-'.]+$").WithMessage("Search query contains invalid characters");

RuleFor(x => x.Count)
    .GreaterThan(0).WithMessage("Count must be greater than 0")
    .LessThanOrEqualTo(10).WithMessage("Count cannot exceed 10");

RuleFor(x => x.Language)
    .Matches(@"^[a-z]{2}$").WithMessage("Language must be a valid 2-letter ISO code")
    .When(x => !string.IsNullOrEmpty(x.Language));
```

**Response Contract:**
```json
{
  "cities": [
    {
      "name": "Kraków",
      "country": "Poland", 
      "latitude": 50.0647,
      "longitude": 19.9450,
      "region": "Lesser Poland Voivodeship",
      "population": 779115
    }
  ]
}
```

### 2. Weather Forecast Endpoint
```
GET /api/weather
```

**Query Parameters:**
- `lat` (required): Latitude (double, -90 to 90)
- `lon` (required): Longitude (double, -180 to 180)
- `days` (optional): Forecast days (int, 1-7, default: 5)

**Request Validation Rules:**
```csharp
// WeatherRequestValidator
RuleFor(x => x.Lat)
    .GreaterThanOrEqualTo(-90).WithMessage("Latitude must be between -90 and 90")
    .LessThanOrEqualTo(90).WithMessage("Latitude must be between -90 and 90");

RuleFor(x => x.Lon)
    .GreaterThanOrEqualTo(-180).WithMessage("Longitude must be between -180 and 180")
    .LessThanOrEqualTo(180).WithMessage("Longitude must be between -180 and 180");

RuleFor(x => x.Days)
    .GreaterThan(0).WithMessage("Days must be greater than 0")
    .LessThanOrEqualTo(7).WithMessage("Days cannot exceed 7");
```

**Response Contract:**
```json
{
  "location": {
    "name": "Kraków",
    "country": "Poland",
    "latitude": 50.0647,
    "longitude": 19.9450,
    "timezone": "Europe/Warsaw"
  },
  "current": {
    "time": "2024-01-15T14:30:00",
    "temperatureC": 15.2,
    "windSpeedKph": 12.6,
    "weatherCode": 3,
    "isDay": true,
    "condition": "Overcast",
    "icon": "04d"
  },
  "daily": [
    {
      "date": "2024-01-15",
      "temperatureMaxC": 18.1,
      "temperatureMinC": 8.3,
      "precipitationProbabilityPct": 20,
      "windSpeedMaxKph": 15.8,
      "weatherCode": 3,
      "condition": "Overcast",
      "icon": "04d"
    }
  ],
  "source": {
    "provider": "open-meteo",
    "model": "best_match"
  }
}
```

## Mapping Plan: Open-Meteo → Our DTOs

### City Search Mapping
```csharp
// OpenMeteoCityResult → CityResult
public static CityResult MapToPublicDto(OpenMeteoCityResult external)
{
    return new CityResult
    {
        Name = external.Name,
        Country = external.Country ?? GetCountryName(external.CountryCode),
        Latitude = Math.Round(external.Latitude, 6),
        Longitude = Math.Round(external.Longitude, 6),
        Region = external.Admin1,
        Population = external.Population
    };
}

// Country code fallback mapping
private static readonly Dictionary<string, string> CountryCodes = new()
{
    ["US"] = "United States", ["CA"] = "Canada", ["GB"] = "United Kingdom",
    ["DE"] = "Germany", ["FR"] = "France", ["PL"] = "Poland", ["ES"] = "Spain",
    ["IT"] = "Italy", ["NL"] = "Netherlands", ["BE"] = "Belgium", ["AT"] = "Austria",
    ["CH"] = "Switzerland", ["CZ"] = "Czech Republic", ["DK"] = "Denmark",
    ["SE"] = "Sweden", ["NO"] = "Norway", ["FI"] = "Finland"
};
```

### Weather Forecast Mapping
```csharp
// OpenMeteoWeatherResponse → WeatherResponse
public static WeatherResponse MapToPublicDto(OpenMeteoWeatherResponse external, CityResult? location = null)
{
    var (currentCondition, currentIcon) = WeatherCodeMapper.GetWeatherInfo(
        external.Current?.WeatherCode ?? 0, 
        external.Current?.IsDay == 1);

    return new WeatherResponse
    {
        Location = location ?? new LocationData
        {
            Name = "Unknown",
            Country = "Unknown", 
            Latitude = external.Latitude,
            Longitude = external.Longitude,
            Timezone = external.Timezone
        },
        Current = new CurrentWeatherData
        {
            Time = external.Current?.Time ?? DateTime.UtcNow,
            TemperatureC = Math.Round(external.Current?.Temperature ?? 0, 1),
            WindSpeedKph = Math.Round((external.Current?.WindSpeed ?? 0) * 3.6, 1),
            WeatherCode = external.Current?.WeatherCode ?? 0,
            IsDay = external.Current?.IsDay == 1,
            Condition = currentCondition,
            Icon = currentIcon
        },
        Daily = MapDailyData(external.Daily),
        Source = new SourceInfo
        {
            Provider = "open-meteo",
            Model = "best_match"
        }
    };
}

private static List<DailyWeatherData> MapDailyData(DailyWeather? daily)
{
    if (daily?.Time == null || daily.Time.Count == 0)
        return [];

    var result = new List<DailyWeatherData>();
    for (int i = 0; i < daily.Time.Count; i++)
    {
        var weatherCode = daily.WeatherCode.ElementAtOrDefault(i);
        var (condition, icon) = WeatherCodeMapper.GetWeatherInfo(weatherCode, true);

        result.Add(new DailyWeatherData
        {
            Date = daily.Time[i].ToString("yyyy-MM-dd"),
            TemperatureMaxC = Math.Round(daily.TemperatureMax.ElementAtOrDefault(i), 1),
            TemperatureMinC = Math.Round(daily.TemperatureMin.ElementAtOrDefault(i), 1),
            PrecipitationProbabilityPct = daily.PrecipitationProbabilityMax.ElementAtOrDefault(i),
            WindSpeedMaxKph = Math.Round(daily.WindSpeedMax.ElementAtOrDefault(i) * 3.6, 1),
            WeatherCode = weatherCode,
            Condition = condition,
            Icon = icon
        });
    }
    return result;
}
```

## Resilience Policy Matrix

### HTTP Client Configuration
```csharp
// Named HttpClient setup with Polly
services.AddHttpClient("OpenMeteoGeocoding", client =>
{
    client.BaseAddress = new Uri("https://geocoding-api.open-meteo.com/");
    client.DefaultRequestHeaders.Add("User-Agent", "WeatherProxy/1.0");
    client.Timeout = TimeSpan.FromSeconds(6); // Overall timeout
})
.AddStandardResilienceHandler(options =>
{
    // Timeout Policy
    options.TotalRequestTimeout = new HttpTimeoutStrategyOptions
    {
        Timeout = TimeSpan.FromSeconds(4)
    };

    // Retry Policy  
    options.Retry = new HttpRetryStrategyOptions
    {
        MaxRetryAttempts = 3,
        Delay = TimeSpan.FromMilliseconds(500),
        BackoffType = DelayBackoffType.Exponential,
        UseJitter = true,
        ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
            .Handle<HttpRequestException>()
            .Handle<TaskCanceledException>()
            .HandleResult(response => 
                response.StatusCode >= HttpStatusCode.InternalServerError ||
                response.StatusCode == HttpStatusCode.RequestTimeout ||
                response.StatusCode == HttpStatusCode.TooManyRequests)
    };

    // Circuit Breaker
    options.CircuitBreaker = new HttpCircuitBreakerStrategyOptions
    {
        FailureRatio = 0.5,
        SamplingDuration = TimeSpan.FromSeconds(30),
        MinimumThroughput = 5,
        BreakDuration = TimeSpan.FromSeconds(15)
    };
});

services.AddHttpClient("OpenMeteoForecast", client =>
{
    client.BaseAddress = new Uri("https://api.open-meteo.com/");
    client.DefaultRequestHeaders.Add("User-Agent", "WeatherProxy/1.0");
    client.Timeout = TimeSpan.FromSeconds(8); // Longer for weather data
})
.AddStandardResilienceHandler(/* same policy as above */);
```

### Retryable Status Codes
| Status Code | Retry | Reason |
|-------------|-------|---------|
| 408 Request Timeout | ✅ | Temporary network issue |
| 429 Too Many Requests | ✅ | Rate limiting (with backoff) |
| 500 Internal Server Error | ✅ | Temporary server issue |
| 502 Bad Gateway | ✅ | Upstream connectivity |
| 503 Service Unavailable | ✅ | Temporary unavailability |
| 504 Gateway Timeout | ✅ | Upstream timeout |
| 400 Bad Request | ❌ | Client error (won't change) |
| 401 Unauthorized | ❌ | Authentication issue |
| 404 Not Found | ❌ | Resource doesn't exist |

## Error Model (RFC7807 ProblemDetails)

### Base Error Response Structure
```csharp
public class ApiProblemDetails : ProblemDetails
{
    public string? TraceId { get; set; }
    public string? CorrelationId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object>? Context { get; set; }
}
```

### Error Examples

#### 400 - Validation Error
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Validation Failed",
  "status": 400,
  "detail": "One or more validation errors occurred.",
  "instance": "/api/cities/search",
  "traceId": "00-80e1afed08e019fc1110464cfa66635c-7a085853906b9681-01",
  "correlationId": "550e8400-e29b-41d4-a716-446655440000",
  "timestamp": "2024-01-15T14:30:00.123Z",
  "errors": {
    "q": ["Search query must be at least 2 characters"]
  }
}
```

#### 404 - City Not Found
```json
{
  "type": "https://example.com/problems/city-not-found",
  "title": "City Not Found",
  "status": 404,
  "detail": "No cities found matching the search criteria 'xyz123'.",
  "instance": "/api/cities/search",
  "traceId": "00-80e1afed08e019fc1110464cfa66635c-7a085853906b9681-01",
  "correlationId": "550e8400-e29b-41d4-a716-446655440000",
  "timestamp": "2024-01-15T14:30:00.123Z",
  "context": {
    "searchQuery": "xyz123",
    "searchParameters": {
      "count": 5,
      "language": "en"
    }
  }
}
```

#### 502 - Upstream Error
```json
{
  "type": "https://example.com/problems/upstream-error",
  "title": "External Service Error",
  "status": 502,
  "detail": "The weather data provider is currently unavailable. Please try again later.",
  "instance": "/api/weather",
  "traceId": "00-80e1afed08e019fc1110464cfa66635c-7a085853906b9681-01",
  "correlationId": "550e8400-e29b-41d4-a716-446655440000",
  "timestamp": "2024-01-15T14:30:00.123Z",
  "context": {
    "upstreamService": "open-meteo.com",
    "upstreamError": "Service temporarily unavailable",
    "retryAfter": "2024-01-15T14:35:00.000Z"
  }
}
```

## Logging/Metrics Plan

### Correlation ID Propagation
```csharp
// Middleware to handle correlation IDs
public class CorrelationIdMiddleware
{
    private const string CorrelationIdHeaderName = "x-correlation-id";
    
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var correlationId = context.Request.Headers[CorrelationIdHeaderName].FirstOrDefault()
            ?? Guid.NewGuid().ToString();
            
        context.Items["CorrelationId"] = correlationId;
        context.Response.Headers[CorrelationIdHeaderName] = correlationId;
        
        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await next(context);
        }
    }
}
```

### Structured Logging Events
```csharp
// City Search Events
Log.Information("City search requested {SearchQuery} {Count} {Language} {CorrelationId}",
    request.Q, request.Count, request.Language, correlationId);

Log.Information("City search completed {SearchQuery} {ResultCount} {Duration}ms {CorrelationId}",
    request.Q, response.Cities.Count, stopwatch.ElapsedMilliseconds, correlationId);

Log.Warning("City search returned no results {SearchQuery} {CorrelationId}",
    request.Q, correlationId);

// Weather Forecast Events  
Log.Information("Weather forecast requested {Latitude} {Longitude} {Days} {CorrelationId}",
    request.Lat, request.Lon, request.Days, correlationId);

Log.Information("Weather forecast completed {Latitude} {Longitude} {Duration}ms {CorrelationId}",
    request.Lat, request.Lon, stopwatch.ElapsedMilliseconds, correlationId);

// External API Events
Log.Information("Calling Open-Meteo API {Endpoint} {CorrelationId}",
    requestUri, correlationId);

Log.Error("Open-Meteo API error {Endpoint} {StatusCode} {Error} {CorrelationId}",
    requestUri, response.StatusCode, error, correlationId);

// Performance Metrics
Log.Information("Request performance {Endpoint} {Duration}ms {StatusCode} {CorrelationId}",
    context.Request.Path, stopwatch.ElapsedMilliseconds, context.Response.StatusCode, correlationId);
```

### Metrics Collection
```csharp
// Custom metrics (if using Application Insights)
public static class Metrics
{
    private static readonly Counter RequestCounter = 
        Meter.CreateCounter<int>("weather_api_requests_total");
    
    private static readonly Histogram RequestDuration = 
        Meter.CreateHistogram<double>("weather_api_request_duration_ms");
    
    public static void RecordRequest(string endpoint, int statusCode, double durationMs)
    {
        RequestCounter.Add(1, new KeyValuePair<string, object?>[]
        {
            new("endpoint", endpoint),
            new("status_code", statusCode)
        });
        
        RequestDuration.Record(durationMs, new KeyValuePair<string, object?>[]
        {
            new("endpoint", endpoint)
        });
    }
}
```

## CORS Configuration

### Startup Configuration
```csharp
// Program.cs - CORS setup
services.AddCors(options =>
{
    options.AddPolicy("LocalDevelopment", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .WithMethods("GET", "OPTIONS")
              .WithHeaders("Content-Type", "x-correlation-id")
              .AllowCredentials()
              .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
    });
});

// Enable CORS in the pipeline
app.UseCors("LocalDevelopment");
```

### Function-Level CORS (if needed)
```csharp
[Function("SearchCities")]
public async Task<HttpResponseData> SearchCities(
    [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "cities/search")] 
    HttpRequestData req)
{
    var response = req.CreateResponse();
    
    // Add CORS headers manually if needed
    response.Headers.Add("Access-Control-Allow-Origin", "http://localhost:4200");
    response.Headers.Add("Access-Control-Allow-Methods", "GET, OPTIONS");
    response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, x-correlation-id");
    
    // ... function implementation
    
    return response;
}
```

## Minimal Test List

### Unit Tests

#### Service Layer Tests
```csharp
// WeatherServiceTests.cs
- GetCitiesAsync_ValidQuery_ReturnsCities
- GetCitiesAsync_EmptyQuery_ThrowsValidationException  
- GetCitiesAsync_ExternalApiError_ThrowsServiceException
- GetCitiesAsync_NoResults_ReturnsEmptyList
- GetWeatherAsync_ValidCoordinates_ReturnsWeatherData
- GetWeatherAsync_InvalidCoordinates_ThrowsValidationException
- GetWeatherAsync_ExternalApiTimeout_ThrowsTimeoutException
- MapCityResult_CompleteData_MapsAllFields
- MapCityResult_MissingCountry_UsesCountryCode
- MapWeatherData_ValidResponse_ConvertsUnitsCorrectly
```

#### Mapping Tests
```csharp
// MappingTests.cs
- MapOpenMeteoCityToPublic_AllFields_MapsCorrectly
- MapOpenMeteoWeatherToPublic_AllFields_MapsCorrectly
- WeatherCodeMapping_AllCodes_ReturnsCorrectDescriptions
- WindSpeedConversion_MetersPerSecond_ConvertsToKilometersPerHour
- TimeZoneConversion_UtcToLocal_FormatsCorrectly
```

#### Validation Tests
```csharp
// ValidationTests.cs
- CitySearchRequest_ValidInput_PassesValidation
- CitySearchRequest_EmptyQuery_FailsValidation
- CitySearchRequest_QueryTooShort_FailsValidation
- CitySearchRequest_QueryTooLong_FailsValidation
- WeatherRequest_ValidCoordinates_PassesValidation
- WeatherRequest_InvalidLatitude_FailsValidation
- WeatherRequest_InvalidLongitude_FailsValidation
- WeatherRequest_InvalidDays_FailsValidation
```

### Integration Tests

#### Function Tests
```csharp
// CityFunctionTests.cs
- SearchCities_ValidQuery_Returns200WithCities
- SearchCities_EmptyQuery_Returns400WithProblemDetails
- SearchCities_NoResults_Returns404WithProblemDetails
- SearchCities_ExternalApiDown_Returns502WithProblemDetails

// WeatherFunctionTests.cs  
- GetWeather_ValidCoordinates_Returns200WithWeatherData
- GetWeather_InvalidCoordinates_Returns400WithProblemDetails
- GetWeather_ExternalApiDown_Returns502WithProblemDetails
```

#### HTTP Client Tests
```csharp
// ExternalApiTests.cs
- OpenMeteoGeocoding_RealApi_ReturnsValidResponse
- OpenMeteoForecast_RealApi_ReturnsValidResponse
- HttpResilience_TransientFailure_RetriesSuccessfully
- HttpResilience_PermanentFailure_FailsAfterRetries
```

## File Map

### Project Structure
```
WeatherProxyApi/
├── WeatherProxyApi.csproj
├── host.json
├── local.settings.json
├── Program.cs                          # DI setup, configuration
│
├── Functions/
│   ├── CityFunctions.cs               # City search endpoint
│   └── WeatherFunctions.cs            # Weather forecast endpoint
│
├── Services/
│   ├── IWeatherService.cs             # Service interface
│   ├── WeatherService.cs              # Service implementation
│   └── Extensions/
│       └── ServiceCollectionExtensions.cs  # DI registration
│
├── Models/
│   ├── Requests/
│   │   ├── CitySearchRequest.cs       # Public API request DTOs
│   │   └── WeatherRequest.cs
│   ├── Responses/
│   │   ├── CitySearchResponse.cs      # Public API response DTOs
│   │   ├── WeatherResponse.cs
│   │   ├── LocationData.cs
│   │   ├── CurrentWeatherData.cs
│   │   ├── DailyWeatherData.cs
│   │   └── SourceInfo.cs
│   ├── External/
│   │   ├── OpenMeteoGeocodingResponse.cs  # External API DTOs
│   │   ├── OpenMeteoWeatherResponse.cs
│   │   ├── OpenMeteoCityResult.cs
│   │   ├── CurrentWeather.cs
│   │   ├── DailyWeather.cs
│   │   └── WeatherUnits.cs
│   ├── Errors/
│   │   └── ApiProblemDetails.cs       # RFC7807 error model
│   └── JsonContext.cs                 # Source generation context
│
├── Validation/
│   ├── CitySearchRequestValidator.cs  # FluentValidation rules
│   └── WeatherRequestValidator.cs
│
├── Utils/
│   ├── WeatherCodeMapper.cs           # WMO code → description/icon
│   ├── UnitConverter.cs               # Temperature, wind speed conversions
│   └── TimeZoneHelper.cs              # Timezone formatting
│
├── Middleware/
│   ├── CorrelationIdMiddleware.cs     # Correlation ID handling
│   ├── ExceptionHandlingMiddleware.cs # Global error handling
│   └── RequestLoggingMiddleware.cs    # Request/response logging
│
├── OpenApi/
│   └── SwaggerConfiguration.cs        # OpenAPI documentation setup
│
└── Tests/
    ├── WeatherProxyApi.Tests.csproj
    ├── Unit/
    │   ├── Services/
    │   │   └── WeatherServiceTests.cs
    │   ├── Validation/
    │   │   ├── CitySearchRequestValidatorTests.cs
    │   │   └── WeatherRequestValidatorTests.cs
    │   ├── Utils/
    │   │   ├── WeatherCodeMapperTests.cs
    │   │   ├── UnitConverterTests.cs
    │   │   └── MappingTests.cs
    │   └── Fixtures/
    │       ├── TestData.cs
    │       └── MockHttpMessageHandler.cs
    └── Integration/
        ├── Functions/
        │   ├── CityFunctionsTests.cs
        │   └── WeatherFunctionsTests.cs
        ├── ExternalApi/
        │   └── OpenMeteoApiTests.cs
        └── TestFixtures/
            ├── WebApplicationFactory.cs
            └── TestContainerSetup.cs
```

### Key Implementation Files

#### 1. Program.cs - Bootstrap & DI
```csharp
var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        // JSON Serialization
        services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.TypeInfoResolverChain.Insert(0, WeatherApiJsonContext.Default);
        });
        
        // HTTP Clients with resilience
        services.AddWeatherApiHttpClients();
        
        // Services
        services.AddScoped<IWeatherService, WeatherService>();
        
        // Validation
        services.AddValidatorsFromAssemblyContaining<CitySearchRequestValidator>();
        
        // Logging
        services.AddSerilog(config => config.WriteTo.Console());
        
        // CORS
        services.AddCors();
    })
    .Build();

host.Run();
```

#### 2. Functions/CityFunctions.cs
```csharp
[Function("SearchCities")]
public async Task<HttpResponseData> SearchCities(
    [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "cities/search")] 
    HttpRequestData req,
    CancellationToken cancellationToken)
{
    var correlationId = req.Headers.GetValues("x-correlation-id").FirstOrDefault() 
        ?? Guid.NewGuid().ToString();
        
    using var activity = ActivitySource.StartActivity("SearchCities");
    activity?.SetTag("correlation-id", correlationId);
    
    // Extract and validate request
    var request = new CitySearchRequest
    {
        Q = req.Query["q"] ?? string.Empty,
        Count = int.TryParse(req.Query["count"], out var count) ? count : 5,
        Language = req.Query["language"] ?? "en"
    };
    
    // Validate
    var validationResult = await _validator.ValidateAsync(request, cancellationToken);
    if (!validationResult.IsValid)
    {
        return await CreateProblemResponse(req, 400, "Validation Failed", 
            validationResult.Errors, correlationId);
    }
    
    // Call service
    var cities = await _weatherService.SearchCitiesAsync(request, cancellationToken);
    
    if (!cities.Any())
    {
        return await CreateProblemResponse(req, 404, "City Not Found", 
            $"No cities found matching '{request.Q}'", correlationId);
    }
    
    var response = req.CreateResponse(HttpStatusCode.OK);
    await response.WriteAsJsonAsync(new CitySearchResponse { Cities = cities });
    response.Headers.Add("x-correlation-id", correlationId);
    
    return response;
}
```

#### 3. Services/WeatherService.cs
```csharp
public class WeatherService : IWeatherService
{
    private readonly HttpClient _geocodingClient;
    private readonly HttpClient _forecastClient;
    private readonly ILogger<WeatherService> _logger;
    
    public async Task<List<CityResult>> SearchCitiesAsync(
        CitySearchRequest request, CancellationToken cancellationToken)
    {
        var url = $"v1/search?name={Uri.EscapeDataString(request.Q)}" +
                  $"&count={request.Count}&language={request.Language}&format=json";
        
        _logger.LogInformation("Calling Open-Meteo Geocoding API {Url}", url);
        
        var response = await _geocodingClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        var openMeteoResponse = JsonSerializer.Deserialize(content, 
            WeatherApiJsonContext.Default.OpenMeteoGeocodingResponse);
        
        return openMeteoResponse?.Results?
            .Select(MapToPublicDto)
            .ToList() ?? [];
    }
    
    private static CityResult MapToPublicDto(OpenMeteoCityResult external) =>
        new()
        {
            Name = external.Name,
            Country = external.Country ?? GetCountryName(external.CountryCode),
            Latitude = Math.Round(external.Latitude, 6),
            Longitude = Math.Round(external.Longitude, 6),
            Region = external.Admin1,
            Population = external.Population
        };
}
```

This comprehensive implementation plan provides a complete roadmap for building the Weather Proxy API with proper error handling, resilience, logging, and testing strategies aligned with the .NET 9 Azure Functions architecture.
