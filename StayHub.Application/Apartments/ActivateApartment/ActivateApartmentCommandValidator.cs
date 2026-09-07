using FluentValidation;

namespace StayHub.Application.Apartments.ActivateApartment;

internal sealed class ActivateApartmentCommandValidator : AbstractValidator<ActivateApartmentCommand>
{
    public ActivateApartmentCommandValidator()
    {
        RuleFor(x => x.ApartmentId).NotEmpty();
    }
}