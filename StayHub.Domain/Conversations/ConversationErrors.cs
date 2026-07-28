using StayHub.Domain.Abstractions;

namespace StayHub.Domain.Conversations;

public static class ConversationErrors
{
    public static readonly Error NotFound = Error.NotFound(
        "Conversation.NotFound",
        "The conversation with the specified identifier was not found");

    public static readonly Error AlreadyExists = Error.Conflict(
        "Conversation.AlreadyExists",
        "A conversation between these participants already exists for this apartment");
}