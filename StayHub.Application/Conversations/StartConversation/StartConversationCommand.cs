using StayHub.Application.Abstractions.Messaging;

namespace StayHub.Application.Conversations.StartConversation;

public sealed record StartConversationCommand(Guid ApartmentId, string InitialMessage) : ICommand<Guid>;