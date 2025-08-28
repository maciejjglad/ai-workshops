# Backend AI Development Rules
***.NET 9 Azure Functions (Isolated Worker) with C# 13***

## Principles
- **API-First**: Design endpoints with clear contracts before implementation
- **Resilience by Default**: Every external call must have timeout, retry, and circuit breaker patterns
- **Minimal Dependencies**: Use built-in .NET features first, add packages only when necessary
- **Source Generation**: Prefer `System.Text.Json` source generators over reflection
- **Explicit Validation**: All inputs validated using `FluentValidation`
- **Dependency Injection**: Use built-in DI container, register all services properly
- **Structured Logging**: Use `Serilog` with correlation IDs for traceability
- **HTTP Standards**: Follow REST conventions, proper status codes, and OpenAPI documentation

## Folder/File Conventions

```
/
├── Functions/
│   ├── CityFunctions.cs        # City search endpoints
│   └── WeatherFunctions.cs     # Weather data endpoints
├── Services/
│   ├── IWeatherService.cs      # Service interfaces
│   └── WeatherService.cs       # Service implementations
├── Models/
│   ├── DTOs/                   # Data transfer objects
│   ├── Requests/               # Request models
│   ├── Responses/              # Response models
│   └── JsonContext.cs          # Source generation context
├── Validation/
│   ├── CitySearchValidator.cs  # Request validators
│   └── WeatherRequestValidator.cs
├── OpenApi/
│   └── SwaggerConfiguration.cs # OpenAPI setup
├── Extensions/
│   └── ServiceCollectionExtensions.cs # DI registration
└── Tests/
    ├── Unit/                   # Unit tests
    ├── Integration/            # Integration tests
    └── TestFixtures/           # Test data/helpers
```

## Coding Conventions

### Naming
- **Classes**: PascalCase (`WeatherService`, `CityFunctions`)
- **Methods**: PascalCase (`GetWeatherAsync`, `SearchCitiesAsync`)
- **Properties**: PascalCase (`Temperature`, `CityName`)
- **Fields**: camelCase with underscore prefix (`_httpClient`, `_logger`)
- **Parameters**: camelCase (`cityName`, `latitude`)
- **Constants**: UPPER_SNAKE_CASE (`MAX_RETRY_ATTEMPTS`)
- **Async Methods**: Always suffix with `Async`

### Error Handling
- Use `Result<T>` pattern or throw specific exceptions
- Always log errors with correlation ID
- Return appropriate HTTP status codes (400, 404, 500, 503)
- Validate all inputs at function entry point
- Handle `HttpRequestException`, `TaskCanceledException`, `JsonException`

### Nullability/Strictness
- Enable nullable reference types: `<Nullable>enable</Nullable>`
- Use `required` keyword for mandatory properties
- Prefer `string?` over `string` for optional values
- Use `ArgumentNullException.ThrowIfNull()` for parameter validation
- Always check external API responses for null values

## Testing Conventions

### Structure
- **Unit Tests**: Test single components in isolation
- **Integration Tests**: Test function endpoints with mocked external dependencies
- **Test Class Naming**: `{ClassUnderTest}Tests` (e.g., `WeatherServiceTests`)
- **Test Method Naming**: `{Method}_{Scenario}_{ExpectedResult}` (e.g., `GetWeatherAsync_ValidCoordinates_ReturnsWeatherData`)

### Tools & Patterns
```csharp
// Use FluentAssertions for readable assertions
result.Should().NotBeNull();
result.Temperature.Should().BeGreaterThan(0);

// Use NSubstitute for mocking
var mockHttpClient = Substitute.For<HttpClient>();
var weatherService = new WeatherService(mockHttpClient, logger);

// Test async methods properly
var result = await weatherService.GetWeatherAsync(lat, lon);
```

### Coverage
- Minimum 80% code coverage
- Test happy path, edge cases, and error scenarios
- Mock all external dependencies (`HttpClient`, external APIs)

## Do/Don't Table

| ✅ DO | ❌ DON'T |
|--------|----------|
| Use `HttpClientFactory` with named clients | Create `HttpClient` instances directly |
| Add resilience policies (`AddStandardResilienceHandler`) | Make HTTP calls without timeout/retry |
| Use `System.Text.Json` with source generators | Use `Newtonsoft.Json` |
| Validate inputs with `FluentValidation` | Skip input validation |
| Log with correlation IDs and structured data | Use `Console.WriteLine` or unstructured logs |
| Return proper HTTP status codes (200, 400, 404, 500) | Always return 200 with error in body |
| Use async/await throughout the pipeline | Block on async calls with `.Result` |
| Register services in DI container | Use static dependencies or new() in functions |
| Use `ILogger<T>` for logging | Use concrete logger types |
| Follow REST conventions for endpoints | Create RPC-style endpoints |
| Document APIs with OpenAPI/Swagger | Leave APIs undocumented |
| Use nullable reference types | Ignore potential null reference issues |

## "When in Doubt" Checklist

**Before Writing Code:**
- [ ] Is this endpoint RESTful and follows HTTP standards?
- [ ] Do I have proper input validation with `FluentValidation`?
- [ ] Are all dependencies registered in DI container?
- [ ] Is there resilience handling for external calls?

**HTTP Client Usage:**
- [ ] Using `HttpClientFactory` with named/typed client?
- [ ] Added timeout and retry policies via `AddStandardResilienceHandler`?
- [ ] Handling `HttpRequestException` and `TaskCanceledException`?
- [ ] Using correlation ID in request headers?

**Error Handling:**
- [ ] Returning appropriate HTTP status codes?
- [ ] Logging errors with correlation ID and context?
- [ ] Validating all inputs at function boundary?
- [ ] Handling null responses from external APIs?

**Performance & Reliability:**
- [ ] Using `System.Text.Json` with source generation?
- [ ] Async all the way down (no blocking calls)?
- [ ] Configured appropriate timeouts (2s connect, 4s overall)?
- [ ] Added circuit breaker for critical external dependencies?

**Testing:**
- [ ] Unit tests for business logic with mocked dependencies?
- [ ] Integration tests for function endpoints?
- [ ] Error scenarios and edge cases covered?
- [ ] Using `FluentAssertions` and `NSubstitute`?

**Documentation:**
- [ ] OpenAPI/Swagger documentation complete?
- [ ] Request/response models properly documented?
- [ ] Error responses documented with examples?
- [ ] CORS configuration matches frontend requirements?
