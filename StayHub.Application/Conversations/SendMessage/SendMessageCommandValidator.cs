using FluentValidation;

namespace StayHub.Application.Conversations.SendMessage;

internal sealed class SendMessageCommandValidator : AbstractValidator<SendMessageCommand>
{
    public SendMessageCommandValidator()
    {
        RuleFor(x => x.ConversationId).NotEmpty();

        RuleFor(x => x.Body)
            .NotEmpty()
            .MaximumLength(4000);
    }
}