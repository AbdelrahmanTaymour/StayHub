using FluentValidation;

namespace StayHub.Application.Users.RefreshAccessToken;

internal sealed class RefreshAccessTokenCommandValidator : AbstractValidator<RefreshAccessTokenCommand>
{
    public RefreshAccessTokenCommandValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty();
    }
}