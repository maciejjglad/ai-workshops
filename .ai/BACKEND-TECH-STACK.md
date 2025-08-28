# Weather Proxy API – TECH STACK

## Runtime
- **.NET 9** + **Azure Functions (Isolated Worker)**, HTTP triggers
- **C# 13**, minimal endpoints style

## Dependencies
- HTTP: `HttpClientFactory` with resilience (Polly via `AddStandardResilienceHandler`)
- DI: built-in
- JSON: `System.Text.Json` + source generators
- Validation: `FluentValidation`
- Mapping: `AutoMapper` (or manual mapping)
- Docs: **Swagger (OpenAPI)** for Azure Functions
- Testing: `xUnit`, `FluentAssertions`, `NSubstitute`
- Logging: `Serilog` (console)

## Endpoints
- `GET /api/cities/search?q={name}`
  - `https://geocoding-api.open-meteo.com/v1/search?name={q}&count=5&language=en&format=json`
- `GET /api/weather?lat={lat}&lon={lon}&days=5`
  - `https://api.open-meteo.com/v1/forecast?latitude={lat}&longitude={lon}&current=temperature_2m,wind_speed_10m,is_day,weather_code&daily=weather_code,temperature_2m_max,temperature_2m_min,precipitation_probability_max,wind_speed_10m_max&timezone=auto&forecast_days={days}`

## Resilience
- Timeouts (e.g., 2s connect, 4s overall)
- Retry (e.g., 3 tries on 5xx/408)
- Circuit breaker (optional)

## CORS
- Allow `http://localhost:4200`
- Methods: GET
- Headers: default + `x-correlation-id`

## Project Structure
- `Functions/CityFunctions.cs`
- `Functions/WeatherFunctions.cs`
- `Services/IWeatherService.cs` (+ `WeatherService.cs`)
- `Models/*` (DTOs + source-gen context)
- `Validation/*`
- `OpenApi/*` (Swagger setup)
- `Tests/*`

## Build & Run (local only)
- Local: `func start` or `dotnet run` (isolated)
