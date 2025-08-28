# WeatherService Integration Notes

## Architecture Overview

The WeatherService implementation follows a clean architecture pattern with clear separation of concerns:

### Layer Structure
```
Functions (Controllers)
    ↓
IWeatherService (Business Logic)
    ↓
IWeatherApiClient (External API Abstraction)
    ↓
HttpClient (Transport Layer)
```

### Key Components

#### 1. **IWeatherApiClient & WeatherApiClient**
- **Purpose**: Low-level HTTP client wrapper for Open-Meteo API
- **Responsibilities**: 
  - URL construction with proper encoding
  - HTTP request/response handling
  - Error classification (4xx vs 5xx)
  - JSON deserialization with source generation
  - Input validation at transport level

#### 2. **IWeatherService & WeatherService**
- **Purpose**: High-level business logic and orchestration
- **Responsibilities**:
  - Request validation and business rules
  - Data transformation through mappers
  - Exception translation for upper layers
  - Optional reverse geocoding for location names
  - Structured logging with correlation

#### 3. **Mapping Helpers**
- **GeocodingMapper**: External geocoding data → Public DTOs
- **WeatherMapper**: External weather data → Public DTOs
- **Features**: 
  - Null-safe transformations
  - Unit conversions (m/s → km/h)
  - Weather code → human descriptions
  - Coordinate precision control (6 decimal places)

## Dependency Injection Registration

### Core Services Registration
```csharp
// In Program.cs or Startup.cs
services.AddWeatherApiServices();        // Business logic services
services.AddWeatherApiHttpClients();     // HTTP clients with resilience
services.AddWeatherApiCors();           // CORS configuration
```

### Detailed Registration (inside AddWeatherApiServices)
```csharp
// Core business logic
services.AddScoped<IWeatherService, WeatherService>();
services.AddScoped<IWeatherApiClient, WeatherApiClient>();

// Validation
services.AddValidatorsFromAssemblyContaining<CitySearchRequestValidator>();

// JSON serialization with source generation
services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, WeatherApiJsonContext.Default);
});
```

### HTTP Client Registration (inside AddWeatherApiHttpClients)
```csharp
// Named clients with different configurations
services.AddHttpClient("OpenMeteoGeocoding", client => { ... })
    .AddStandardResilienceHandler(options => { ... });

services.AddHttpClient("OpenMeteoForecast", client => { ... })
    .AddStandardResilienceHandler(options => { ... });
```

## Resilience Policies

### Timeout Strategy
- **Geocoding**: 4s request timeout, 6s total timeout
- **Weather**: 6s request timeout, 8s total timeout
- **Rationale**: Weather data is more complex and may take longer

### Retry Strategy
- **Max Attempts**: 3 (including initial attempt)
- **Backoff**: Exponential with jitter
- **Base Delay**: 500ms
- **Retryable Conditions**:
  - `HttpRequestException` (network issues)
  - `TaskCanceledException` (timeouts)
  - HTTP 5xx status codes
  - HTTP 408 (Request Timeout)
  - HTTP 429 (Too Many Requests)

### Circuit Breaker
- **Failure Threshold**: 50% failure rate
- **Sampling Window**: 30 seconds
- **Minimum Throughput**: 5 requests
- **Break Duration**: 15 seconds
- **Purpose**: Prevent cascade failures when external API is down

## Error Handling Strategy

### Error Classification
1. **Terminal Errors (No Retry)**:
   - HTTP 400, 401, 403, 404, 422
   - `JsonException` (malformed response)
   - Invalid input parameters

2. **Transient Errors (Retry)**:
   - HTTP 5xx status codes
   - Network connectivity issues
   - Request timeouts

3. **Rate Limiting**:
   - HTTP 429: Exponential backoff then return 503

### Exception Translation
```
External API Error → ExternalApiException → InvalidOperationException → HTTP Problem Details
```

This provides clean separation where:
- Functions only handle `InvalidOperationException`
- Internal service exceptions don't leak to API consumers
- HTTP status codes are properly mapped

## Data Transformation

### Unit Conversions
- **Wind Speed**: m/s → km/h (multiply by 3.6, round to 1 decimal)
- **Temperature**: °C (round to 1 decimal, validate -100°C to +60°C)
- **Coordinates**: Round to 6 decimal places (~0.1m precision)

### Weather Code Mapping
- WMO codes → Human-readable descriptions + weather icons
- Day/night awareness for icon selection
- Fallback to "Unknown" for unmapped codes

### Data Quality
- Null-safe array access with fallbacks
- Empty result handling without errors
- Optional field handling (population, admin regions)
- Country name resolution (prefer full name, fallback to code mapping)

## Testing Strategy

### Unit Tests
- **Mappers**: Input/output transformation verification
- **Utils**: Unit conversion accuracy, coordinate validation
- **Services**: Business logic with mocked dependencies

### Integration Tests
- **HTTP Clients**: Real API calls (rate-limited)
- **Resilience**: Circuit breaker behavior, retry patterns
- **Error Scenarios**: Timeout handling, malformed responses

### Test Data
- Use real city names and coordinates for realistic testing
- Mock external API responses for consistent unit tests
- Test edge cases: null island (0,0), extreme coordinates, Unicode city names

## Configuration Options

### appsettings.json Example
```json
{
  "WeatherApi": {
    "GeocodingBaseUrl": "https://geocoding-api.open-meteo.com/",
    "ForecastBaseUrl": "https://api.open-meteo.com/",
    "UserAgent": "WeatherProxy/1.0",
    "MaxCityResults": 10,
    "MaxForecastDays": 16,
    "DefaultLanguage": "en",
    "Timeouts": {
      "GeocodingClientTimeout": "00:00:06",
      "ForecastClientTimeout": "00:00:08",
      "GeocodingRequestTimeout": "00:00:04",
      "ForecastRequestTimeout": "00:00:06"
    },
    "Retry": {
      "MaxAttempts": 3,
      "BaseDelay": "00:00:00.500",
      "UseJitter": true
    },
    "CircuitBreaker": {
      "FailureRatio": 0.5,
      "SamplingDuration": "00:00:30",
      "MinimumThroughput": 5,
      "BreakDuration": "00:00:15"
    }
  }
}
```

## Monitoring & Observability

### Structured Logging
- Correlation ID propagation through all layers
- Request/response timing
- External API call details
- Error details with context
- Performance metrics (response times, success rates)

### Key Metrics to Monitor
- Request success rate by endpoint
- Average response times
- Circuit breaker state transitions
- Retry attempt frequencies
- External API error rates by status code

### Log Levels
- **Information**: Successful operations, timing
- **Warning**: Retries, fallbacks, data quality issues
- **Error**: Service failures, external API errors
- **Debug**: Detailed request/response data (non-production)

This architecture provides a robust, observable, and maintainable integration with the Open-Meteo API while following .NET best practices.
