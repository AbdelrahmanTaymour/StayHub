using FluentValidation;

namespace StayHub.Application.Apartments.DeactivateApartment;

internal sealed class DeactivateApartmentCommandValidator : AbstractValidator<DeactivateApartmentCommand>
{
    public DeactivateApartmentCommandValidator()
    {
        RuleFor(x => x.ApartmentId).NotEmpty();
    }
}