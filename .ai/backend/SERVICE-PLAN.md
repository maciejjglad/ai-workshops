# WeatherService Integration Plan
**Open-Meteo API Integration Architecture**

## API Endpoints & Query Parameters

### 1. Geocoding API
**Base URL:** `https://geocoding-api.open-meteo.com/v1/search`

**Required Parameters:**
- `name` (string): City name to search
- `count` (integer): Maximum number of results (1-100, default: 10)
- `language` (string): Language code (ISO 639-1, default: "en")
- `format` (string): Response format ("json", default: "json")

**Optional Parameters:**
- `country` (string): Country code filter (ISO 3166-1 alpha-2)
- `admin1` (string): State/province filter
- `admin2` (string): County/district filter
- `admin3` (string): Municipality filter
- `admin4` (string): City district filter

**Example Request:**
```
GET https://geocoding-api.open-meteo.com/v1/search?name=Kraków&count=5&language=en&format=json
```

### 2. Weather Forecast API
**Base URL:** `https://api.open-meteo.com/v1/forecast`

**Required Parameters:**
- `latitude` (float): Latitude in decimal degrees (-90 to 90)
- `longitude` (float): Longitude in decimal degrees (-180 to 180)

**Current Weather Variables:**
- `temperature_2m`: Temperature at 2 meters above ground (°C)
- `wind_speed_10m`: Wind speed at 10 meters above ground (m/s)
- `is_day`: Day or night indicator (0=night, 1=day)
- `weather_code`: WMO weather interpretation code

**Daily Weather Variables:**
- `weather_code`: WMO weather interpretation code
- `temperature_2m_max`: Maximum daily temperature at 2m (°C)
- `temperature_2m_min`: Minimum daily temperature at 2m (°C)
- `precipitation_probability_max`: Maximum precipitation probability (%)
- `wind_speed_10m_max`: Maximum daily wind speed at 10m (m/s)

**Configuration Parameters:**
- `timezone` (string): Timezone ("auto" or IANA timezone)
- `forecast_days` (integer): Number of forecast days (1-16, default: 7)
- `past_days` (integer): Number of past days (0-92, default: 0)

**Example Request:**
```
GET https://api.open-meteo.com/v1/forecast?latitude=50.0647&longitude=19.9450&current=temperature_2m,wind_speed_10m,is_day,weather_code&daily=weather_code,temperature_2m_max,temperature_2m_min,precipitation_probability_max,wind_speed_10m_max&timezone=auto&forecast_days=5
```

## Required Variables & Units

### Current Weather Variables
| Variable | Unit | Description | Required |
|----------|------|-------------|----------|
| `temperature_2m` | °C | Air temperature at 2m height | ✅ |
| `wind_speed_10m` | m/s | Wind speed at 10m height | ✅ |
| `is_day` | 0/1 | Daylight indicator | ✅ |
| `weather_code` | WMO code | Weather condition code | ✅ |
| `time` | ISO 8601 | Timestamp | ✅ |

### Daily Weather Variables
| Variable | Unit | Description | Required |
|----------|------|-------------|----------|
| `time` | YYYY-MM-DD | Date | ✅ |
| `weather_code` | WMO code | Weather condition code | ✅ |
| `temperature_2m_max` | °C | Maximum daily temperature | ✅ |
| `temperature_2m_min` | °C | Minimum daily temperature | ✅ |
| `precipitation_probability_max` | % | Max precipitation probability | ✅ |
| `wind_speed_10m_max` | m/s | Maximum daily wind speed | ✅ |

### Geocoding Variables
| Variable | Type | Description | Required |
|----------|------|-------------|----------|
| `id` | integer | Location ID | ✅ |
| `name` | string | Location name | ✅ |
| `latitude` | float | Latitude | ✅ |
| `longitude` | float | Longitude | ✅ |
| `country_code` | string | ISO country code | ✅ |
| `country` | string | Country name | ❌ |
| `admin1` | string | State/province | ❌ |
| `timezone` | string | IANA timezone | ❌ |
| `population` | integer | Population count | ❌ |
| `elevation` | float | Elevation in meters | ❌ |

## Error Taxonomy & Handling Strategy

### HTTP Status Code Classification

#### 2xx Success
| Code | Description | Action |
|------|-------------|--------|
| 200 | OK | Process response normally |

#### 4xx Client Errors (Terminal - Do Not Retry)
| Code | Description | Handling Strategy |
|------|-------------|-------------------|
| 400 | Bad Request | Validate input parameters, return 400 to client |
| 401 | Unauthorized | Log error, return 502 (unexpected - no auth required) |
| 403 | Forbidden | Log error, return 502 (service misconfiguration) |
| 404 | Not Found | For geocoding: return empty results; For weather: return 404 |
| 422 | Unprocessable Entity | Validate coordinates/parameters, return 400 to client |
| 429 | Too Many Requests | Implement exponential backoff, then return 503 |

#### 5xx Server Errors (Transient - Retry)
| Code | Description | Handling Strategy |
|------|-------------|-------------------|
| 500 | Internal Server Error | Retry with exponential backoff, return 502 after retries |
| 502 | Bad Gateway | Retry with exponential backoff, return 502 after retries |
| 503 | Service Unavailable | Retry with exponential backoff, return 503 after retries |
| 504 | Gateway Timeout | Retry with exponential backoff, return 504 after retries |

#### Network/Connection Errors
| Error Type | Description | Handling Strategy |
|------------|-------------|-------------------|
| `HttpRequestException` | Network connectivity issues | Retry with exponential backoff |
| `TaskCanceledException` | Request timeout | Retry with exponential backoff |
| `SocketException` | DNS/network issues | Retry with exponential backoff |

#### Content/Parsing Errors
| Error Type | Description | Handling Strategy |
|------------|-------------|-------------------|
| `JsonException` | Malformed JSON response | Log detailed error, return 502 (terminal) |
| Empty Response | No content returned | Log warning, return empty results or 502 |
| Schema Mismatch | Missing required fields | Log warning, use defaults where possible |

### Retry Configuration
- **Max Attempts:** 3
- **Base Delay:** 500ms
- **Backoff:** Exponential with jitter
- **Total Timeout:** 4s (geocoding), 6s (weather)
- **Circuit Breaker:** 50% failure rate over 30s window

## Field Mapping Table

### Geocoding: External → Internal
| External Field | Internal Field | Transformation | Notes |
|----------------|----------------|----------------|-------|
| `name` | `Name` | Direct copy | Required |
| `country` | `Country` | Use country or map country_code | Fallback to country code mapping |
| `country_code` | - | Map to country name | Used for fallback |
| `latitude` | `Latitude` | Round to 6 decimal places | Precision control |
| `longitude` | `Longitude` | Round to 6 decimal places | Precision control |
| `admin1` | `Region` | Direct copy | Optional |
| `population` | `Population` | Direct copy | Optional |
| `timezone` | - | Not exposed in public API | Internal use only |

### Weather: External → Internal
| External Field | Internal Field | Transformation | Notes |
|----------------|----------------|----------------|-------|
| `latitude` | `Location.Latitude` | Round to 6 decimal places | From response metadata |
| `longitude` | `Location.Longitude` | Round to 6 decimal places | From response metadata |
| `timezone` | `Location.Timezone` | Direct copy | IANA timezone |
| `current.time` | `Current.Time` | Parse ISO 8601 | DateTime object |
| `current.temperature_2m` | `Current.TemperatureC` | Round to 1 decimal | °C temperature |
| `current.wind_speed_10m` | `Current.WindSpeedKph` | Convert m/s → km/h | Unit conversion |
| `current.is_day` | `Current.IsDay` | Convert int to bool | 1=true, 0=false |
| `current.weather_code` | `Current.WeatherCode` | Direct copy | WMO code |
| `current.weather_code` | `Current.Condition` | Map to description | Weather description |
| `current.weather_code` | `Current.Icon` | Map to icon code | Weather icon |
| `daily.time[i]` | `Daily[i].Date` | Format YYYY-MM-DD | Date string |
| `daily.temperature_2m_max[i]` | `Daily[i].TemperatureMaxC` | Round to 1 decimal | °C temperature |
| `daily.temperature_2m_min[i]` | `Daily[i].TemperatureMinC` | Round to 1 decimal | °C temperature |
| `daily.precipitation_probability_max[i]` | `Daily[i].PrecipitationProbabilityPct` | Direct copy | Percentage |
| `daily.wind_speed_10m_max[i]` | `Daily[i].WindSpeedMaxKph` | Convert m/s → km/h | Unit conversion |
| `daily.weather_code[i]` | `Daily[i].WeatherCode` | Direct copy | WMO code |
| `daily.weather_code[i]` | `Daily[i].Condition` | Map to description | Weather description |
| `daily.weather_code[i]` | `Daily[i].Icon` | Map to icon code | Weather icon |

## Unit Conversion & Data Processing

### Wind Speed Conversion
- **Input:** meters per second (m/s)
- **Output:** kilometers per hour (km/h)
- **Formula:** `km/h = m/s × 3.6`
- **Precision:** Round to 1 decimal place
- **Example:** 5.0 m/s → 18.0 km/h

### Temperature Processing
- **Input:** Celsius (°C)
- **Output:** Celsius (°C)
- **Processing:** Round to 1 decimal place
- **Range Validation:** -100°C to +60°C (sanity check)
- **Future:** Optional Fahrenheit conversion support

### Coordinate Precision
- **Input:** Double precision coordinates
- **Output:** 6 decimal places (~0.1m precision)
- **Rationale:** Balance between precision and payload size

### Weather Code Mapping
**WMO Weather Interpretation Codes:**
| Code Range | Condition | Icon (Day/Night) | Description |
|------------|-----------|------------------|-------------|
| 0 | Clear sky | 01d/01n | Clear sky |
| 1 | Mainly clear | 02d/02n | Mainly clear |
| 2 | Partly cloudy | 03d/03n | Partly cloudy |
| 3 | Overcast | 04d/04d | Overcast |
| 45, 48 | Fog | 50d/50d | Fog and depositing rime fog |
| 51, 53, 55 | Drizzle | 09d/09d | Drizzle: Light, moderate, dense |
| 61, 63, 65 | Rain | 10d/10d | Rain: Slight, moderate, heavy |
| 71, 73, 75 | Snow | 13d/13d | Snow fall: Slight, moderate, heavy |
| 80, 81, 82 | Rain showers | 09d/09d | Rain showers: Slight, moderate, violent |
| 85, 86 | Snow showers | 13d/13d | Snow showers: Slight, heavy |
| 95 | Thunderstorm | 11d/11d | Thunderstorm: Slight or moderate |
| 96, 99 | Thunderstorm with hail | 11d/11d | Thunderstorm with hail |

### Timezone Handling
- **API Parameter:** `timezone=auto`
- **Response:** Local timezone for coordinates
- **Processing:** Use timezone for local time display
- **Storage:** Store as IANA timezone string
- **Conversion:** No conversion needed (API handles local time)

### Date/Time Processing
- **Current Time:** ISO 8601 format from API
- **Daily Dates:** YYYY-MM-DD format
- **Storage:** Use DateTime for current, string for daily dates
- **Timezone:** Respect local timezone from API response

## Test Matrix

### Happy Path Tests
| Test Case | Input | Expected Output | Assertions |
|-----------|-------|----------------|------------|
| **Geocoding - Single City** | `q="London", count=1` | 1 city result | Name, country, coordinates present |
| **Geocoding - Multiple Cities** | `q="Springfield", count=5` | 2-5 cities | Multiple results with different states/countries |
| **Geocoding - Non-English** | `q="Kraków", language="pl"` | Polish city | Proper Unicode handling |
| **Weather - Valid Coordinates** | `lat=51.5074, lon=-0.1278, days=5` | Weather data | Current + 5 daily forecasts |
| **Weather - Minimal Days** | `lat=51.5074, lon=-0.1278, days=1` | Weather data | Current + 1 daily forecast |
| **Weather - Maximum Days** | `lat=51.5074, lon=-0.1278, days=7` | Weather data | Current + 7 daily forecasts |

### Error Scenario Tests
| Test Case | Simulation | Expected Behavior | Verification |
|-----------|------------|-------------------|--------------|
| **5xx Transient Errors** | Mock 503 → 503 → 200 | Retry and succeed | 3 attempts, final success |
| **5xx Persistent Errors** | Mock 500 → 500 → 500 | Exhaust retries | Return 502 after 3 attempts |
| **4xx Terminal Errors** | Mock 400 Bad Request | No retry | Single attempt, return 400 |
| **4xx Geocoding Not Found** | `q="NonexistentCity123"` | Empty results | Return 404 with empty cities array |
| **Network Timeout** | Mock slow response | Timeout and retry | Respect timeout limits |
| **Connection Refused** | Mock connection failure | Retry pattern | Handle connection errors |

### Data Quality Tests
| Test Case | Input/Scenario | Expected Behavior | Validation |
|-----------|----------------|-------------------|------------|
| **Malformed JSON** | Invalid JSON response | Log error, return 502 | No crash, proper error response |
| **Missing Required Fields** | JSON without required fields | Use defaults where possible | Graceful degradation |
| **Invalid Coordinates** | `lat=999, lon=999` | Validation error | Return 400 with validation message |
| **Extreme Weather Values** | Temperature > 100°C | Log warning, pass through | Data validation logging |
| **Empty API Response** | Empty results array | Return empty results | Handle empty data gracefully |
| **Unicode City Names** | Cities with special characters | Proper encoding | UTF-8 handling verification |

### Performance Tests
| Test Case | Scenario | Target | Measurement |
|-----------|----------|---------|-------------|
| **Response Time** | Normal requests | < 800ms | End-to-end timing |
| **Concurrent Requests** | 10 parallel requests | No degradation | Latency percentiles |
| **Circuit Breaker** | Trigger failure threshold | Open circuit | Monitor state transitions |
| **Memory Usage** | Process large responses | Stable memory | Memory leak detection |

### Integration Tests
| Test Case | Environment | Verification | Notes |
|-----------|-------------|--------------|-------|
| **Real API - Geocoding** | Live Open-Meteo API | Valid city results | Rate limiting aware |
| **Real API - Weather** | Live Open-Meteo API | Valid weather data | Coordinate validation |
| **Rate Limiting** | Rapid requests | Proper 429 handling | Respect API limits |
| **Service Availability** | API downtime simulation | Circuit breaker activation | Resilience verification |

## File/Class Architecture

### Service Layer
```
Services/
├── IWeatherService.cs
│   └── SearchCitiesAsync(CitySearchRequest) : List<CityResult>
│   └── GetWeatherAsync(WeatherRequest) : WeatherResponse
├── WeatherService.cs
│   └── Primary implementation class
├── IWeatherApiClient.cs (Optional abstraction)
│   └── GetGeocodingAsync(string, int, string) : OpenMeteoGeocodingResponse
│   └── GetForecastAsync(double, double, int) : OpenMeteoWeatherResponse
└── WeatherApiClient.cs (Optional dedicated client)
    └── HTTP client wrapper with resilience
```

### Data Models
```
Models/External/
├── OpenMeteoGeocodingResponse.cs
│   └── Results: List<OpenMeteoCityResult>
├── OpenMeteoCityResult.cs
│   └── All geocoding fields from API
├── OpenMeteoWeatherResponse.cs
│   └── Current, Daily, metadata
├── CurrentWeather.cs
│   └── Current weather data structure
├── DailyWeather.cs
│   └── Daily forecast arrays
└── WeatherUnits.cs
    └── Unit information from API

Models/Responses/
├── CitySearchResponse.cs
│   └── Cities: List<CityResult>
├── CityResult.cs
│   └── Public city information
├── WeatherResponse.cs
│   └── Location, Current, Daily, Source
├── LocationData.cs
│   └── Location metadata
├── CurrentWeatherData.cs
│   └── Current weather for public API
└── DailyWeatherData.cs
    └── Daily forecast for public API
```

### Utilities & Mappers
```
Utils/
├── WeatherCodeMapper.cs
│   └── GetWeatherInfo(int, bool) : (string, string)
├── UnitConverter.cs
│   └── ConvertWindSpeedToKmh(double) : double
│   └── ConvertToFahrenheit(double) : double (future)
├── CountryCodeMapper.cs
│   └── GetCountryName(string) : string
└── CoordinateValidator.cs
    └── ValidateCoordinates(double, double) : bool
```

### Error Handling
```
Errors/
├── WeatherServiceException.cs
│   └── Base exception for service errors
├── ExternalApiException.cs
│   └── Upstream API error wrapper
├── ValidationException.cs
│   └── Input validation errors
└── TimeoutException.cs
    └── Request timeout specific errors
```

### Configuration
```
Configuration/
├── WeatherApiOptions.cs
│   └── Base URLs, timeouts, retry policies
├── ResilienceOptions.cs
│   └── Circuit breaker, retry configuration
└── MappingProfile.cs (if using AutoMapper)
    └── External to internal mappings
```

### Testing Structure
```
Tests/Unit/Services/
├── WeatherServiceTests.cs
│   └── Business logic testing with mocked HTTP
├── WeatherCodeMapperTests.cs
│   └── Weather code mapping verification
└── UnitConverterTests.cs
    └── Unit conversion accuracy tests

Tests/Integration/
├── WeatherApiIntegrationTests.cs
│   └── Real API calls (rate limited)
├── ResilienceTests.cs
│   └── Circuit breaker, retry behavior
└── ErrorHandlingTests.cs
    └── Error scenario verification
```

This comprehensive integration plan provides a complete roadmap for implementing the WeatherService with proper error handling, data transformation, and testing strategies.
