using FluentValidation;

namespace StayHub.Application.Apartments.RemoveApartmentAvailabilityBlock;

internal sealed class
    RemoveApartmentAvailabilityBlockCommandValidator : AbstractValidator<RemoveApartmentAvailabilityBlockCommand>
{
    public RemoveApartmentAvailabilityBlockCommandValidator()
    {
        RuleFor(x => x.BlockId).NotEmpty();
    }
}