using FluentValidation;

namespace StayHub.Application.Users.LogOutUser;

internal sealed class LogOutUserCommandValidator : AbstractValidator<LogOutUserCommand>
{
    public LogOutUserCommandValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty();
    }
}