using StayHub.Application.Abstractions.Messaging;

namespace StayHub.Application.Conversations.MarkConversationAsRead;

public sealed record MarkConversationAsReadCommand(Guid ConversationId, Guid RequestedByUserId) : ICommand;