using StayHub.Application.Abstractions.Messaging;

namespace StayHub.Application.Conversations.StartConversation;

public sealed record StartConversationCommand(Guid ApartmentId, Guid GuestId, string InitialMessage) : ICommand<Guid>;