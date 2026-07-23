using FluentValidation;
using StayHub.Application.Shared;

namespace StayHub.Application.Apartments.CreateApartment;

public class CreateApartmentCommandValidator : AbstractValidator<CreateApartmentCommand>
{
    public CreateApartmentCommandValidator()
    {
        RuleFor(x => x.OwnerId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.Street).NotEmpty();
        RuleFor(x => x.City).NotEmpty();
        RuleFor(x => x.Country).NotEmpty();

        RuleFor(x => x.PriceAmount).GreaterThan(0);
        RuleFor(x => x.PriceCurrency)
            .NotEmpty()
            .Must(ValidationHelper.BeValidCurrency)
            .WithMessage("Unsupported currency code.");

        RuleFor(x => x.CleaningFeeAmount).GreaterThanOrEqualTo(0);
        RuleFor(x => x.CleaningFeeCurrency)
            .NotEmpty()
            .Must(ValidationHelper.BeValidCurrency)
            .WithMessage("Unsupported currency code.");
    }
}