using FluentValidation;

namespace StayHub.Application.Apartments.RemoveApartmentImage;

public class RemoveApartmentImageCommandValidator : AbstractValidator<RemoveApartmentImageCommand>
{
    public RemoveApartmentImageCommandValidator()
    {
        RuleFor(x => x.ImageId).NotEmpty();
    }
}