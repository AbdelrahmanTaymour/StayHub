using StayHub.Application.Abstractions.Messaging;

namespace StayHub.Application.Conversations.SendMessage;

public sealed record SendMessageCommand(Guid ConversationId, string Body) : ICommand<Guid>;