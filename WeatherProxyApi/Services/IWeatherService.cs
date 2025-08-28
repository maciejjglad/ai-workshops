using WeatherProxyApi.Models.Requests;
using WeatherProxyApi.Models.Responses;

namespace WeatherProxyApi.Services;

public interface IWeatherService
{
    Task<List<CityResult>> SearchCitiesAsync(CitySearchRequest request, CancellationToken cancellationToken = default);
    Task<WeatherResponse> GetWeatherAsync(WeatherRequest request, CancellationToken cancellationToken = default);
}
