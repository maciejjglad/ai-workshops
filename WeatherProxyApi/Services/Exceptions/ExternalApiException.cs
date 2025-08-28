namespace WeatherProxyApi.Services.Exceptions;

/// <summary>
/// Exception thrown when external API calls fail
/// </summary>
public class ExternalApiException : WeatherServiceException
{
    public int? HttpStatusCode { get; }
    public string? ResponseContent { get; }

    public ExternalApiException() : base()
    {
    }

    public ExternalApiException(string message) : base(message)
    {
    }

    public ExternalApiException(string message, Exception innerException) : base(message, innerException)
    {
    }

    public ExternalApiException(string message, int httpStatusCode, string? responseContent = null) 
        : base(message)
    {
        HttpStatusCode = httpStatusCode;
        ResponseContent = responseContent;
    }

    public ExternalApiException(string message, int httpStatusCode, string? responseContent, Exception innerException) 
        : base(message, innerException)
    {
        HttpStatusCode = httpStatusCode;
        ResponseContent = responseContent;
    }
}
