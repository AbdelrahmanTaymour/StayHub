using StayHub.Domain.Conversations;

namespace StayHub.Application.UnitTests.Conversations;

internal static class ConversationData
{
    public static Conversation Start(Guid apartmentId, Guid guestId, Guid ownerId, Guid? bookingId = null)
    {
        return Conversation.Start(apartmentId, bookingId, guestId, ownerId, DateTime.UtcNow).Value;
    }
}