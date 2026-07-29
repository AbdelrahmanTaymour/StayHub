using FluentValidation;

namespace StayHub.Application.Apartments.SearchApartments;

public sealed class SearchApartmentsQueryValidator : AbstractValidator<SearchApartmentsQuery>
{
    public SearchApartmentsQueryValidator()
    {
        When(x => x.MinPrice.HasValue, () =>
        {
            RuleFor(x => x.MinPrice)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Minimum price cannot be negative.");
        });

        When(x => x.MaxPrice.HasValue, () =>
        {
            RuleFor(x => x.MaxPrice)
                .GreaterThan(0)
                .WithMessage("Maximum price must be greater than zero.");
        });

        When(x => x.MinPrice.HasValue && x.MaxPrice.HasValue, () =>
        {
            RuleFor(x => x.MaxPrice)
                .GreaterThanOrEqualTo(x => x.MinPrice!.Value)
                .WithMessage("Maximum price must be greater than or equal to minimum price.");
        });

        When(x => x.Start.HasValue && x.End.HasValue, () =>
        {
            RuleFor(x => x.End)
                .GreaterThanOrEqualTo(x => x.Start!.Value)
                .WithMessage("End date must be on or after the start date.");
        });

        When(x => x.Start.HasValue, () =>
        {
            RuleFor(x => x.Start)
                .GreaterThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow))
                .WithMessage("Start date cannot be in the past.");
        });

        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Page must be at least 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("Page size must be between 1 and 100.");
    }
}