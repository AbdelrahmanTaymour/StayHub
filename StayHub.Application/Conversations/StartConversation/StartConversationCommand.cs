using StayHub.Application.Abstractions.Messaging;

namespace StayHub.Application.Conversations.StartConversation;

public sealed record StartConversationCommand(
    Guid ApartmentId,
    Guid? BookingId,
    string InitialMessage) : ICommand<Guid>;