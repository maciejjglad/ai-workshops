using FluentValidation;
using WeatherProxyApi.Models.Requests;

namespace WeatherProxyApi.Validation;

public class WeatherRequestValidator : AbstractValidator<WeatherRequest>
{
    public WeatherRequestValidator()
    {
        RuleFor(x => x.Lat)
            .GreaterThanOrEqualTo(-90).WithMessage("Latitude must be between -90 and 90")
            .LessThanOrEqualTo(90).WithMessage("Latitude must be between -90 and 90");

        RuleFor(x => x.Lon)
            .GreaterThanOrEqualTo(-180).WithMessage("Longitude must be between -180 and 180")
            .LessThanOrEqualTo(180).WithMessage("Longitude must be between -180 and 180");

        RuleFor(x => x.Days)
            .GreaterThan(0).WithMessage("Days must be greater than 0")
            .LessThanOrEqualTo(7).WithMessage("Days cannot exceed 7");
    }
}
