using StayHub.Domain.Abstractions;

namespace StayHub.Domain.Conversations;

public static class ConversationErrors
{
    public static Error NotFound = new(
        "Conversation.NotFound",
        "The conversation with the specified identifier was not found");

    public static Error AlreadyExists = new(
        "Conversation.AlreadyExists",
        "A conversation between these participants already exists for this apartment");
}