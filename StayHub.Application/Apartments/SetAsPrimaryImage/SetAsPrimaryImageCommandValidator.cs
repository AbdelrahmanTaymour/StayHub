using FluentValidation;

namespace StayHub.Application.Apartments.SetAsPrimaryImage;

internal sealed class SetAsPrimaryImageCommandValidator : AbstractValidator<SetAsPrimaryImageCommand>
{
    public SetAsPrimaryImageCommandValidator()
    {
        RuleFor(x => x.ApartmentId).NotEmpty();
        RuleFor(x => x.ImageId).NotEmpty();
    }
}