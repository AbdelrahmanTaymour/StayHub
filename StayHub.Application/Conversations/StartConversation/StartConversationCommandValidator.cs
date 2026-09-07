using FluentValidation;

namespace StayHub.Application.Conversations.StartConversation;

internal sealed class StartConversationCommandValidator : AbstractValidator<StartConversationCommand>
{
    public StartConversationCommandValidator()
    {
        RuleFor(x => x.ApartmentId).NotEmpty();

        RuleFor(x => x.InitialMessage)
            .NotEmpty()
            .MaximumLength(4000);
    }
}