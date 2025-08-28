# Weather Proxy API Documentation
**OpenAPI 3.0 Specification**

## Overview
This Weather Proxy API provides a simple interface to search for cities and retrieve weather forecasts by proxying requests to the Open-Meteo API service.

## Base Information
- **Base URL**: `http://localhost:7071/api` (Azure Functions local development)
- **Production Base URL**: `https://{function-app-name}.azurewebsites.net/api`
- **API Version**: 1.0.0
- **Protocol**: HTTPS (production), HTTP (local development)

## CORS Configuration
- **Allowed Origins**: `http://localhost:4200` (Angular development server)
- **Allowed Methods**: `GET`, `OPTIONS`
- **Allowed Headers**: `Content-Type`, `x-correlation-id`
- **Max Age**: 600 seconds (10 minutes)

## Rate Limits
- **Default**: 100 requests per minute per IP
- **Burst**: 10 requests per second
- **Note**: Rate limiting is handled by Azure Functions hosting platform

---

## OpenAPI 3.0 YAML Specification

```yaml
openapi: 3.0.3
info:
  title: Weather Proxy API
  description: |
    A simple weather API that provides city search and weather forecast capabilities
    by proxying requests to the Open-Meteo API service.
    
    ## Features
    - Search for cities by name with fuzzy matching
    - Get current weather and multi-day forecasts
    - Normalized responses with consistent units
    - RFC7807 compliant error handling
    
    ## Data Sources
    - **Geocoding**: Open-Meteo Geocoding API
    - **Weather**: Open-Meteo Forecast API
  version: 1.0.0
  contact:
    name: Weather Proxy API Support
    url: https://github.com/your-org/weather-proxy-api
  license:
    name: MIT
    url: https://opensource.org/licenses/MIT

servers:
  - url: http://localhost:7071/api
    description: Local development server
  - url: https://{function-app-name}.azurewebsites.net/api
    description: Production server
    variables:
      function-app-name:
        default: weather-proxy-api
        description: Azure Functions app name

paths:
  /cities/search:
    get:
      summary: Search for cities
      description: |
        Search for cities by name using fuzzy matching. Returns a list of matching cities
        with their coordinates and administrative information.
        
        **Use Cases:**
        - Find cities for weather lookup
        - Resolve ambiguous city names (e.g., "Springfield")
        - Get precise coordinates for weather requests
      operationId: searchCities
      tags:
        - Cities
      parameters:
        - name: q
          in: query
          required: true
          description: City name to search for (minimum 2 characters)
          schema:
            type: string
            minLength: 2
            maxLength: 100
            pattern: '^[\p{L}\p{N}\s\-''.]+$'
          examples:
            simple:
              value: "London"
              summary: Simple city search
            ambiguous:
              value: "Springfield"
              summary: Ambiguous city name
            unicode:
              value: "Kraków"
              summary: City with Unicode characters
        - name: count
          in: query
          required: false
          description: Maximum number of results to return
          schema:
            type: integer
            minimum: 1
            maximum: 10
            default: 5
          example: 5
        - name: language
          in: query
          required: false
          description: Language code for localized results (ISO 639-1)
          schema:
            type: string
            pattern: '^[a-z]{2}$'
            default: "en"
          examples:
            english:
              value: "en"
            polish:
              value: "pl"
            german:
              value: "de"
        - name: x-correlation-id
          in: header
          required: false
          description: Correlation ID for request tracing
          schema:
            type: string
            format: uuid
          example: "550e8400-e29b-41d4-a716-446655440000"
      responses:
        '200':
          description: Cities found successfully
          headers:
            x-correlation-id:
              description: Correlation ID for request tracing
              schema:
                type: string
                format: uuid
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/CitySearchResponse'
              examples:
                single_city:
                  summary: Single city result
                  value:
                    cities:
                      - name: "London"
                        country: "United Kingdom"
                        latitude: 51.50853
                        longitude: -0.12574
                        region: "England"
                        population: 8982000
                multiple_cities:
                  summary: Multiple cities (ambiguous search)
                  value:
                    cities:
                      - name: "Springfield"
                        country: "United States"
                        latitude: 39.92961
                        longitude: -83.80882
                        region: "Ohio"
                        population: 58662
                      - name: "Springfield"
                        country: "United States"
                        latitude: 37.21533
                        longitude: -93.29824
                        region: "Missouri"
                        population: 169176
                      - name: "Springfield"
                        country: "United States"
                        latitude: 42.10148
                        longitude: -72.58981
                        region: "Massachusetts"
                        population: 155929
        '400':
          description: Invalid request parameters
          content:
            application/problem+json:
              schema:
                $ref: '#/components/schemas/ProblemDetails'
              examples:
                validation_error:
                  summary: Validation error
                  value:
                    type: "https://tools.ietf.org/html/rfc7231#section-6.5.1"
                    title: "Validation Failed"
                    status: 400
                    detail: "One or more validation errors occurred."
                    instance: "/api/cities/search"
                    traceId: "00-80e1afed08e019fc1110464cfa66635c-7a085853906b9681-01"
                    correlationId: "550e8400-e29b-41d4-a716-446655440000"
                    timestamp: "2024-01-15T14:30:00.123Z"
                    errors:
                      q: ["Search query must be at least 2 characters"]
        '404':
          description: No cities found
          content:
            application/problem+json:
              schema:
                $ref: '#/components/schemas/ProblemDetails'
              examples:
                no_results:
                  summary: No cities found
                  value:
                    type: "https://example.com/problems/city-not-found"
                    title: "City Not Found"
                    status: 404
                    detail: "No cities found matching the search criteria 'xyz123'."
                    instance: "/api/cities/search"
                    traceId: "00-80e1afed08e019fc1110464cfa66635c-7a085853906b9681-01"
                    correlationId: "550e8400-e29b-41d4-a716-446655440000"
                    timestamp: "2024-01-15T14:30:00.123Z"
                    context:
                      searchQuery: "xyz123"
                      searchParameters:
                        count: 5
                        language: "en"
        '502':
          description: External service error
          content:
            application/problem+json:
              schema:
                $ref: '#/components/schemas/ProblemDetails'
              examples:
                upstream_error:
                  summary: Upstream service error
                  value:
                    type: "https://example.com/problems/upstream-error"
                    title: "External Service Error"
                    status: 502
                    detail: "The geocoding service is currently unavailable. Please try again later."
                    instance: "/api/cities/search"
                    traceId: "00-80e1afed08e019fc1110464cfa66635c-7a085853906b9681-01"
                    correlationId: "550e8400-e29b-41d4-a716-446655440000"
                    timestamp: "2024-01-15T14:30:00.123Z"
                    context:
                      upstreamService: "open-meteo.com"
                      upstreamError: "Service temporarily unavailable"

  /weather:
    get:
      summary: Get weather forecast
      description: |
        Get current weather conditions and daily forecast for specified coordinates.
        
        **Use Cases:**
        - Get weather for a specific location
        - Current conditions with hourly precision
        - Multi-day forecast (1-7 days)
        - Normalized units (°C, km/h)
      operationId: getWeather
      tags:
        - Weather
      parameters:
        - name: lat
          in: query
          required: true
          description: Latitude in decimal degrees
          schema:
            type: number
            format: double
            minimum: -90
            maximum: 90
          examples:
            london:
              value: 51.5074
              summary: London, UK
            krakow:
              value: 50.0647
              summary: Kraków, Poland
            sydney:
              value: -33.8688
              summary: Sydney, Australia
        - name: lon
          in: query
          required: true
          description: Longitude in decimal degrees
          schema:
            type: number
            format: double
            minimum: -180
            maximum: 180
          examples:
            london:
              value: -0.1278
            krakow:
              value: 19.9450
            sydney:
              value: 151.2093
        - name: days
          in: query
          required: false
          description: Number of forecast days to return
          schema:
            type: integer
            minimum: 1
            maximum: 7
            default: 5
          example: 5
        - name: x-correlation-id
          in: header
          required: false
          description: Correlation ID for request tracing
          schema:
            type: string
            format: uuid
          example: "550e8400-e29b-41d4-a716-446655440000"
      responses:
        '200':
          description: Weather forecast retrieved successfully
          headers:
            x-correlation-id:
              description: Correlation ID for request tracing
              schema:
                type: string
                format: uuid
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/WeatherResponse'
              examples:
                london_weather:
                  summary: London weather forecast
                  value:
                    location:
                      name: "London"
                      country: "United Kingdom"
                      latitude: 51.5074
                      longitude: -0.1278
                      timezone: "Europe/London"
                    current:
                      time: "2024-01-15T14:30:00"
                      temperatureC: 15.2
                      windSpeedKph: 12.6
                      weatherCode: 3
                      isDay: true
                      condition: "Overcast"
                      icon: "04d"
                    daily:
                      - date: "2024-01-15"
                        temperatureMaxC: 18.1
                        temperatureMinC: 8.3
                        precipitationProbabilityPct: 20
                        windSpeedMaxKph: 15.8
                        weatherCode: 3
                        condition: "Overcast"
                        icon: "04d"
                      - date: "2024-01-16"
                        temperatureMaxC: 22.3
                        temperatureMinC: 12.1
                        precipitationProbabilityPct: 80
                        windSpeedMaxKph: 18.7
                        weatherCode: 61
                        condition: "Rain"
                        icon: "10d"
                    source:
                      provider: "open-meteo"
                      model: "best_match"
        '400':
          description: Invalid coordinates or parameters
          content:
            application/problem+json:
              schema:
                $ref: '#/components/schemas/ProblemDetails'
              examples:
                invalid_coordinates:
                  summary: Invalid coordinates
                  value:
                    type: "https://tools.ietf.org/html/rfc7231#section-6.5.1"
                    title: "Validation Failed"
                    status: 400
                    detail: "One or more validation errors occurred."
                    instance: "/api/weather"
                    traceId: "00-80e1afed08e019fc1110464cfa66635c-7a085853906b9681-01"
                    correlationId: "550e8400-e29b-41d4-a716-446655440000"
                    timestamp: "2024-01-15T14:30:00.123Z"
                    errors:
                      lat: ["Latitude must be between -90 and 90"]
                      lon: ["Longitude must be between -180 and 180"]
        '502':
          description: External weather service error
          content:
            application/problem+json:
              schema:
                $ref: '#/components/schemas/ProblemDetails'
              examples:
                weather_service_error:
                  summary: Weather service error
                  value:
                    type: "https://example.com/problems/upstream-error"
                    title: "External Service Error"
                    status: 502
                    detail: "The weather data provider is currently unavailable. Please try again later."
                    instance: "/api/weather"
                    traceId: "00-80e1afed08e019fc1110464cfa66635c-7a085853906b9681-01"
                    correlationId: "550e8400-e29b-41d4-a716-446655440000"
                    timestamp: "2024-01-15T14:30:00.123Z"
                    context:
                      upstreamService: "open-meteo.com"
                      upstreamError: "Service temporarily unavailable"
                      retryAfter: "2024-01-15T14:35:00.000Z"

components:
  schemas:
    CitySearchResponse:
      type: object
      required:
        - cities
      properties:
        cities:
          type: array
          items:
            $ref: '#/components/schemas/CityResult'
          description: List of matching cities
          maxItems: 10
      example:
        cities:
          - name: "London"
            country: "United Kingdom"
            latitude: 51.50853
            longitude: -0.12574
            region: "England"
            population: 8982000

    CityResult:
      type: object
      required:
        - name
        - country
        - latitude
        - longitude
      properties:
        name:
          type: string
          description: City name
          example: "London"
        country:
          type: string
          description: Country name
          example: "United Kingdom"
        latitude:
          type: number
          format: double
          description: Latitude in decimal degrees (6 decimal precision)
          minimum: -90
          maximum: 90
          example: 51.50853
        longitude:
          type: number
          format: double
          description: Longitude in decimal degrees (6 decimal precision)
          minimum: -180
          maximum: 180
          example: -0.12574
        region:
          type: string
          description: Administrative region/state/province
          nullable: true
          example: "England"
        population:
          type: integer
          description: Population count
          nullable: true
          minimum: 0
          example: 8982000

    WeatherResponse:
      type: object
      required:
        - location
        - current
        - daily
        - source
      properties:
        location:
          $ref: '#/components/schemas/LocationData'
        current:
          $ref: '#/components/schemas/CurrentWeatherData'
        daily:
          type: array
          items:
            $ref: '#/components/schemas/DailyWeatherData'
          description: Daily weather forecast
          minItems: 1
          maxItems: 7
        source:
          $ref: '#/components/schemas/SourceInfo'

    LocationData:
      type: object
      required:
        - name
        - country
        - latitude
        - longitude
        - timezone
      properties:
        name:
          type: string
          description: Location name (city or region)
          example: "London"
        country:
          type: string
          description: Country name
          example: "United Kingdom"
        latitude:
          type: number
          format: double
          description: Latitude in decimal degrees
          example: 51.5074
        longitude:
          type: number
          format: double
          description: Longitude in decimal degrees
          example: -0.1278
        timezone:
          type: string
          description: IANA timezone identifier
          example: "Europe/London"

    CurrentWeatherData:
      type: object
      required:
        - time
        - temperatureC
        - windSpeedKph
        - weatherCode
        - isDay
        - condition
        - icon
      properties:
        time:
          type: string
          format: date-time
          description: Current observation time (ISO 8601)
          example: "2024-01-15T14:30:00"
        temperatureC:
          type: number
          format: double
          description: Temperature in Celsius (1 decimal precision)
          example: 15.2
        windSpeedKph:
          type: number
          format: double
          description: Wind speed in kilometers per hour (1 decimal precision)
          example: 12.6
        weatherCode:
          type: integer
          description: WMO weather interpretation code
          minimum: 0
          maximum: 99
          example: 3
        isDay:
          type: boolean
          description: Whether it's currently day or night
          example: true
        condition:
          type: string
          description: Human-readable weather condition
          example: "Overcast"
        icon:
          type: string
          description: Weather icon code (OpenWeatherMap compatible)
          pattern: '^[0-9]{2}[dn]$'
          example: "04d"

    DailyWeatherData:
      type: object
      required:
        - date
        - temperatureMaxC
        - temperatureMinC
        - precipitationProbabilityPct
        - windSpeedMaxKph
        - weatherCode
        - condition
        - icon
      properties:
        date:
          type: string
          format: date
          description: Forecast date (YYYY-MM-DD)
          example: "2024-01-15"
        temperatureMaxC:
          type: number
          format: double
          description: Maximum daily temperature in Celsius
          example: 18.1
        temperatureMinC:
          type: number
          format: double
          description: Minimum daily temperature in Celsius
          example: 8.3
        precipitationProbabilityPct:
          type: integer
          description: Maximum precipitation probability percentage
          minimum: 0
          maximum: 100
          example: 20
        windSpeedMaxKph:
          type: number
          format: double
          description: Maximum daily wind speed in km/h
          example: 15.8
        weatherCode:
          type: integer
          description: WMO weather interpretation code
          minimum: 0
          maximum: 99
          example: 3
        condition:
          type: string
          description: Human-readable weather condition
          example: "Overcast"
        icon:
          type: string
          description: Weather icon code
          pattern: '^[0-9]{2}[dn]$'
          example: "04d"

    SourceInfo:
      type: object
      required:
        - provider
        - model
      properties:
        provider:
          type: string
          description: Weather data provider
          example: "open-meteo"
        model:
          type: string
          description: Weather model used
          example: "best_match"

    ProblemDetails:
      type: object
      required:
        - type
        - title
        - status
      properties:
        type:
          type: string
          format: uri
          description: Problem type identifier
          example: "https://tools.ietf.org/html/rfc7231#section-6.5.1"
        title:
          type: string
          description: Short, human-readable summary
          example: "Validation Failed"
        status:
          type: integer
          description: HTTP status code
          minimum: 100
          maximum: 599
          example: 400
        detail:
          type: string
          description: Human-readable explanation
          example: "One or more validation errors occurred."
        instance:
          type: string
          format: uri
          description: URI reference for this specific problem
          example: "/api/cities/search"
        traceId:
          type: string
          description: Unique trace identifier
          example: "00-80e1afed08e019fc1110464cfa66635c-7a085853906b9681-01"
        correlationId:
          type: string
          format: uuid
          description: Correlation ID for request tracing
          example: "550e8400-e29b-41d4-a716-446655440000"
        timestamp:
          type: string
          format: date-time
          description: Error occurrence timestamp
          example: "2024-01-15T14:30:00.123Z"
        errors:
          type: object
          description: Validation errors by field
          additionalProperties:
            type: array
            items:
              type: string
          example:
            q: ["Search query must be at least 2 characters"]
            count: ["Count must be greater than 0"]
        context:
          type: object
          description: Additional context information
          additionalProperties: true
          example:
            searchQuery: "xyz123"
            upstreamService: "open-meteo.com"

  headers:
    CorrelationId:
      description: Correlation ID for request tracing
      schema:
        type: string
        format: uuid
      example: "550e8400-e29b-41d4-a716-446655440000"

tags:
  - name: Cities
    description: City search and geocoding operations
  - name: Weather
    description: Weather forecast operations

externalDocs:
  description: Open-Meteo API Documentation
  url: https://open-meteo.com/en/docs
```

## Usage Examples

### Example 1: Search for Cities
```bash
# Search for London
curl "http://localhost:7071/api/cities/search?q=London&count=3" \
  -H "x-correlation-id: $(uuidgen)"

# Search with language preference
curl "http://localhost:7071/api/cities/search?q=Kraków&language=pl" \
  -H "x-correlation-id: $(uuidgen)"
```

### Example 2: Get Weather Forecast
```bash
# Get 5-day forecast for London
curl "http://localhost:7071/api/weather?lat=51.5074&lon=-0.1278&days=5" \
  -H "x-correlation-id: $(uuidgen)"

# Get 3-day forecast for Kraków
curl "http://localhost:7071/api/weather?lat=50.0647&lon=19.9450&days=3" \
  -H "x-correlation-id: $(uuidgen)"
```

## Testing with Swagger UI

1. **Local Development**: Navigate to `http://localhost:7071/api/swagger/ui`
2. **Interactive Testing**: Use the Swagger UI to test endpoints with various parameters
3. **Schema Validation**: Verify request/response formats match the specification

## Error Handling Notes

### Common Error Scenarios
- **400 Bad Request**: Invalid query parameters, validation failures
- **404 Not Found**: No cities found for search query
- **502 Bad Gateway**: Upstream Open-Meteo API errors
- **500 Internal Server Error**: Unexpected application errors

### Error Response Format
All errors follow RFC7807 Problem Details standard with:
- Consistent structure across all endpoints
- Correlation ID for tracing
- Detailed validation error messages
- Context information for debugging

## Rate Limiting & Performance

### Expected Response Times
- **City Search**: < 300ms (cached geocoding)
- **Weather Forecast**: < 500ms (real-time data)
- **Total Request**: < 800ms (including network overhead)

### Monitoring & Observability
- All requests logged with correlation IDs
- Structured logging for Azure Application Insights
- Performance metrics tracked automatically
- Error rates monitored per endpoint

---

*This documentation is automatically generated and updated with each API deployment.*
