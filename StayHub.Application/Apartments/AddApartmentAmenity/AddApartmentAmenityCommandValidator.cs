using FluentValidation;

namespace StayHub.Application.Apartments.AddApartmentAmenity;

public class AddApartmentAmenityCommandValidator : AbstractValidator<AddApartmentAmenityCommand>
{
    public AddApartmentAmenityCommandValidator()
    {
        RuleFor(x => x.ApartmentId).NotEmpty();

        RuleFor(x => x.Amenity)
            .IsInEnum()
            .WithMessage("Selected amenity is not supported.");
    }
}