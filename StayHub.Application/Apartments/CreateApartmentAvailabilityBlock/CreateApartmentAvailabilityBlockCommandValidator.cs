using FluentValidation;

namespace StayHub.Application.Apartments.CreateApartmentAvailabilityBlock;

public class
    CreateApartmentAvailabilityBlockCommandValidator : AbstractValidator<CreateApartmentAvailabilityBlockCommand>
{
    public CreateApartmentAvailabilityBlockCommandValidator()
    {
        RuleFor(x => x.ApartmentId)
            .NotEmpty();

        RuleFor(x => x.RequestedByUserId)
            .NotEmpty();

        RuleFor(x => x.Start)
            .GreaterThanOrEqualTo(DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Start date cannot be in the past.");

        RuleFor(x => x.End)
            .GreaterThanOrEqualTo(x => x.Start)
            .WithMessage("End date must be on or after the start date.");

        RuleFor(x => x.Reason)
            .IsInEnum()
            .WithMessage("Selected unavailability reason is not valid.");
    }
}