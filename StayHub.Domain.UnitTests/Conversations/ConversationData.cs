using StayHub.Domain.Conversations;

namespace StayHub.Domain.UnitTests.Conversations;

public class ConversationData
{
    public static Conversation Start(
        Guid? initiatorId = null,
        Guid? recipientId = null,
        Guid? apartmentId = null,
        Guid? bookingId = null)
    {
        return Conversation.Start(
            initiatorId ?? Guid.CreateVersion7(),
            recipientId ?? Guid.CreateVersion7(),
            apartmentId,
            bookingId,
            DateTime.UtcNow).Value;
    }
}