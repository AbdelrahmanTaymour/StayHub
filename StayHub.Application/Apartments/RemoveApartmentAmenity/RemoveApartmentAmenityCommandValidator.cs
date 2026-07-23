using FluentValidation;

namespace StayHub.Application.Apartments.RemoveApartmentAmenity;

public class RemoveApartmentAmenityCommandValidator : AbstractValidator<RemoveApartmentAmenityCommand>
{
    public RemoveApartmentAmenityCommandValidator()
    {
        RuleFor(x => x.ApartmentId).NotEmpty();

        RuleFor(x => x.Amenity)
            .IsInEnum()
            .WithMessage("Selected amenity is not exist.");
    }
}