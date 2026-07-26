using StayHub.Application.Abstractions.Messaging;

namespace StayHub.Application.Conversations.SendMessage;

public sealed record SendMessageCommand(Guid ConversationId, Guid SenderId, string Body) : ICommand<Guid>;