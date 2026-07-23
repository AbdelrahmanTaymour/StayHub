using FluentValidation;
using StayHub.Application.Shared;

namespace StayHub.Application.Apartments.UpdateApartment;

public class UpdateApartmentCommandValidator : AbstractValidator<UpdateApartmentCommand>
{
    public UpdateApartmentCommandValidator()
    {
        RuleFor(x => x.ApartmentId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(2000);

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