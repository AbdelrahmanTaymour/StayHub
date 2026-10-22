using StayHub.Application.Abstractions.Messaging;

namespace StayHub.Application.Conversations.StartConversation;

public sealed record StartConversationCommand(
    Guid RecipientId,
    Guid? ApartmentId,
    Guid? BookingId,
    string InitialMessage) : ICommand<Guid>;