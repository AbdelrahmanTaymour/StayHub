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

    public static readonly Error CannotMessageSelf = Error.Validation(
        "Conversation:CannotMessageSelf",
        "You cannot message yourself");

    public static readonly Error NotAuthorized = Error.Forbidden(
        "Conversation.NotAuthorized",
        "You are not authorized to perform this action");
}