using FluentValidation;

namespace StayHub.Application.Users.LogInUser;

internal sealed class LogInUserCommandValidator : AbstractValidator<LogInUserCommand>
{
    public LogInUserCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}