namespace WeatherProxyApi.Configuration;

/// <summary>
/// Configuration options for Open-Meteo API integration
/// </summary>
public class WeatherApiOptions
{
    public const string SectionName = "WeatherApi";

    /// <summary>
    /// Base URL for the geocoding API
    /// </summary>
    public string GeocodingBaseUrl { get; set; } = "https://geocoding-api.open-meteo.com/";

    /// <summary>
    /// Base URL for the weather forecast API
    /// </summary>
    public string ForecastBaseUrl { get; set; } = "https://api.open-meteo.com/";

    /// <summary>
    /// User agent string for API requests
    /// </summary>
    public string UserAgent { get; set; } = "WeatherProxy/1.0";

    /// <summary>
    /// Maximum number of cities to return from geocoding search
    /// </summary>
    public int MaxCityResults { get; set; } = 10;

    /// <summary>
    /// Maximum number of forecast days to request
    /// </summary>
    public int MaxForecastDays { get; set; } = 16;

    /// <summary>
    /// Default language for geocoding requests
    /// </summary>
    public string DefaultLanguage { get; set; } = "en";

    /// <summary>
    /// Timeout configuration
    /// </summary>
    public TimeoutOptions Timeouts { get; set; } = new();

    /// <summary>
    /// Retry configuration
    /// </summary>
    public RetryOptions Retry { get; set; } = new();

    /// <summary>
    /// Circuit breaker configuration
    /// </summary>
    public CircuitBreakerOptions CircuitBreaker { get; set; } = new();
}

/// <summary>
/// Timeout configuration options
/// </summary>
public class TimeoutOptions
{
    /// <summary>
    /// Overall HTTP client timeout for geocoding requests
    /// </summary>
    public TimeSpan GeocodingClientTimeout { get; set; } = TimeSpan.FromSeconds(6);

    /// <summary>
    /// Overall HTTP client timeout for weather forecast requests
    /// </summary>
    public TimeSpan ForecastClientTimeout { get; set; } = TimeSpan.FromSeconds(8);

    /// <summary>
    /// Per-request timeout for geocoding requests
    /// </summary>
    public TimeSpan GeocodingRequestTimeout { get; set; } = TimeSpan.FromSeconds(4);

    /// <summary>
    /// Per-request timeout for weather forecast requests
    /// </summary>
    public TimeSpan ForecastRequestTimeout { get; set; } = TimeSpan.FromSeconds(6);
}

/// <summary>
/// Retry configuration options
/// </summary>
public class RetryOptions
{
    /// <summary>
    /// Maximum number of retry attempts
    /// </summary>
    public int MaxAttempts { get; set; } = 3;

    /// <summary>
    /// Base delay between retry attempts
    /// </summary>
    public TimeSpan BaseDelay { get; set; } = TimeSpan.FromMilliseconds(500);

    /// <summary>
    /// Whether to use jitter in retry delays
    /// </summary>
    public bool UseJitter { get; set; } = true;
}

/// <summary>
/// Circuit breaker configuration options
/// </summary>
public class CircuitBreakerOptions
{
    /// <summary>
    /// Failure ratio threshold to open the circuit (0.0 to 1.0)
    /// </summary>
    public double FailureRatio { get; set; } = 0.5;

    /// <summary>
    /// Time window for sampling failures
    /// </summary>
    public TimeSpan SamplingDuration { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Minimum number of requests required before circuit breaker can activate
    /// </summary>
    public int MinimumThroughput { get; set; } = 5;

    /// <summary>
    /// Duration to keep the circuit open before attempting to close it
    /// </summary>
    public TimeSpan BreakDuration { get; set; } = TimeSpan.FromSeconds(15);
}
