using FluentValidation;

namespace StayHub.Application.Apartments.AddApartmentAmenity;

internal sealed class AddApartmentAmenityCommandValidator : AbstractValidator<AddApartmentAmenityCommand>
{
    public AddApartmentAmenityCommandValidator()
    {
        RuleFor(x => x.ApartmentId).NotEmpty();

        RuleFor(x => x.Amenity)
            .IsInEnum()
            .WithMessage("Selected amenity is not supported.");
    }
}