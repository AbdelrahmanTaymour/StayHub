using FluentValidation;

namespace StayHub.Application.Conversations.StartConversation;

internal sealed class StartConversationCommandValidator
    : AbstractValidator<StartConversationCommand>
{
    public StartConversationCommandValidator()
    {
        RuleFor(x => x.RecipientId)
            .NotEmpty();

        RuleFor(x => x.ApartmentId)
            .Must(id => id is null || id != Guid.Empty)
            .WithMessage("Apartment ID must be a valid ID when provided.");

        RuleFor(x => x.BookingId)
            .Must(id => id is null || id != Guid.Empty)
            .WithMessage("Booking ID must be a valid ID when provided.");

        RuleFor(x => x.InitialMessage)
            .NotEmpty()
            .MaximumLength(4000);
    }
}