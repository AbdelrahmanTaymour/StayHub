using StayHub.Domain.Conversations;

namespace StayHub.Domain.UnitTests.Conversations;

public class ConversationData
{
    public static Conversation Start(
        Guid? apartmentId = null,
        Guid? bookingId = null,
        Guid? guestId = null,
        Guid? ownerId = null)
    {
        return Conversation.Start(
            apartmentId ?? Guid.CreateVersion7(),
            bookingId,
            guestId ?? Guid.CreateVersion7(),
            ownerId ?? Guid.CreateVersion7(),
            DateTime.UtcNow).Value;
    }
}