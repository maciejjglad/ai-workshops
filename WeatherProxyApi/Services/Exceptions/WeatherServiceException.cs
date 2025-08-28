namespace WeatherProxyApi.Services.Exceptions;

/// <summary>
/// Base exception for weather service operations
/// </summary>
public class WeatherServiceException : Exception
{
    public WeatherServiceException() : base()
    {
    }

    public WeatherServiceException(string message) : base(message)
    {
    }

    public WeatherServiceException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
