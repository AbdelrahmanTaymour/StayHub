using FluentValidation;

namespace StayHub.Application.Users.LogOutUser;

public class LogOutUserCommandValidator : AbstractValidator<LogOutUserCommand>
{
    public LogOutUserCommandValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty();
    }
}