using FluentValidation;
using WeatherProxyApi.Models.Requests;

namespace WeatherProxyApi.Validation;

public class CitySearchRequestValidator : AbstractValidator<CitySearchRequest>
{
    public CitySearchRequestValidator()
    {
        RuleFor(x => x.Q)
            .NotEmpty().WithMessage("Search query is required")
            .MinimumLength(2).WithMessage("Search query must be at least 2 characters")
            .MaximumLength(100).WithMessage("Search query cannot exceed 100 characters")
            .Matches(@"^[\p{L}\p{N}\s\-'.]+$").WithMessage("Search query contains invalid characters");

        RuleFor(x => x.Count)
            .GreaterThan(0).WithMessage("Count must be greater than 0")
            .LessThanOrEqualTo(10).WithMessage("Count cannot exceed 10");

        RuleFor(x => x.Language)
            .Matches(@"^[a-z]{2}$").WithMessage("Language must be a valid 2-letter ISO code")
            .When(x => !string.IsNullOrEmpty(x.Language));
    }
}
