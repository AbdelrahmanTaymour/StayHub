using FluentValidation;

namespace StayHub.Application.Apartments.RemoveApartmentImage;

internal sealed class RemoveApartmentImageCommandValidator : AbstractValidator<RemoveApartmentImageCommand>
{
    public RemoveApartmentImageCommandValidator()
    {
        RuleFor(x => x.ImageId).NotEmpty();
    }
}