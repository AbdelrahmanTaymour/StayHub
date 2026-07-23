using FluentValidation;

namespace StayHub.Application.Apartments.ActivateApartment;

public class ActivateApartmentCommandValidator : AbstractValidator<ActivateApartmentCommand>
{
    public ActivateApartmentCommandValidator()
    {
        RuleFor(x => x.ApartmentId).NotEmpty();
    }
}