using StayHub.Domain.Abstractions;

namespace StayHub.Domain.Conversations;

public static class MessageErrors
{
    public static readonly Error NotFound = Error.NotFound(
        "Message.NotFound",
        "The message with the specified identifier was not found");

    public static readonly Error AlreadyRead = Error.Conflict(
        "Message.AlreadyRead",
        "The message has already been marked as read");

    public static readonly Error NotAuthorized = Error.Unauthorized(
        "Message.NotAuthorized",
        "Only participants in this conversation can send messages");
}