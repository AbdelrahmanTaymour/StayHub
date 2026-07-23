using FluentValidation;

namespace StayHub.Application.Apartments.RemoveApartmentAvailabilityBlock;

public class
    RemoveApartmentAvailabilityBlockCommandValidator : AbstractValidator<RemoveApartmentAvailabilityBlockCommand>
{
    public RemoveApartmentAvailabilityBlockCommandValidator()
    {
        RuleFor(x => x.BlockId).NotEmpty();
    }
}