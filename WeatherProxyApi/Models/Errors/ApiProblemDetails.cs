using Microsoft.AspNetCore.Mvc;

namespace WeatherProxyApi.Models.Errors;

public class ApiProblemDetails : ProblemDetails
{
    public string? TraceId { get; set; }
    public string? CorrelationId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object>? Context { get; set; }
}
