using FluentValidation;

namespace StayHub.Application.Apartments.DeactivateApartment;

public class DeactivateApartmentCommandValidator : AbstractValidator<DeactivateApartmentCommand>
{
    public DeactivateApartmentCommandValidator()
    {
        RuleFor(x => x.ApartmentId).NotEmpty();
    }
}