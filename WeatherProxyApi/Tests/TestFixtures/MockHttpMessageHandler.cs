using System.Net;
using System.Text;

namespace WeatherProxyApi.Tests.TestFixtures;

/// <summary>
/// Mock HTTP message handler for testing HTTP client behavior
/// </summary>
public class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly Queue<HttpResponseMessage> _responses = new();
    private readonly List<HttpRequestMessage> _requests = new();

    public IReadOnlyList<HttpRequestMessage> Requests => _requests.AsReadOnly();

    public void AddResponse(HttpStatusCode statusCode, string content = "", string mediaType = "application/json")
    {
        var response = new HttpResponseMessage(statusCode);
        if (!string.IsNullOrEmpty(content))
        {
            response.Content = new StringContent(content, Encoding.UTF8, mediaType);
        }
        _responses.Enqueue(response);
    }

    public void AddResponse(HttpResponseMessage response)
    {
        _responses.Enqueue(response);
    }

    public void AddSuccessResponse(string jsonContent)
    {
        AddResponse(HttpStatusCode.OK, jsonContent);
    }

    public void AddErrorResponse(HttpStatusCode statusCode, string errorContent = "")
    {
        AddResponse(statusCode, errorContent);
    }

    public void AddTimeoutResponse()
    {
        _responses.Enqueue(new HttpResponseMessage(HttpStatusCode.RequestTimeout));
    }

    public void AddServerErrorResponse()
    {
        AddResponse(HttpStatusCode.InternalServerError, "Internal server error");
    }

    public void AddNotFoundResponse()
    {
        AddResponse(HttpStatusCode.NotFound, "Not found");
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, 
        CancellationToken cancellationToken)
    {
        _requests.Add(request);

        if (_responses.Count == 0)
        {
            throw new InvalidOperationException("No more responses configured for mock HTTP handler");
        }

        var response = _responses.Dequeue();
        return Task.FromResult(response);
    }

    public void VerifyRequestCount(int expectedCount)
    {
        if (_requests.Count != expectedCount)
        {
            throw new InvalidOperationException(
                $"Expected {expectedCount} requests, but received {_requests.Count}");
        }
    }

    public void VerifyRequestUrl(int requestIndex, string expectedUrl)
    {
        if (requestIndex >= _requests.Count)
        {
            throw new InvalidOperationException(
                $"Request index {requestIndex} is out of range. Total requests: {_requests.Count}");
        }

        var actualUrl = _requests[requestIndex].RequestUri?.ToString();
        if (!actualUrl?.Contains(expectedUrl) == true)
        {
            throw new InvalidOperationException(
                $"Expected URL to contain '{expectedUrl}', but was '{actualUrl}'");
        }
    }

    public void Reset()
    {
        _responses.Clear();
        _requests.Clear();
    }
}
