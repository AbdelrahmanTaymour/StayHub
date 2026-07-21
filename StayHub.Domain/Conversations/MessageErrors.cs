using StayHub.Domain.Abstractions;

namespace StayHub.Domain.Conversations;

public static class MessageErrors
{
    public static Error NotFound = new(
        "Message.NotFound",
        "The message with the specified identifier was not found");

    public static Error AlreadyRead = new(
        "Message.AlreadyRead",
        "The message has already been marked as read");
}