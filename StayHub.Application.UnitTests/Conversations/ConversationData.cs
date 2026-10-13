using StayHub.Domain.Conversations;

namespace StayHub.Application.UnitTests.Conversations;

internal static class ConversationData
{
    public static Conversation Start(
        Guid initiatorId,
        Guid recipientId,
        Guid? apartmentId = null,
        Guid? bookingId = null)
    {
        return Conversation.Start(initiatorId, recipientId, apartmentId, bookingId, DateTime.UtcNow).Value;
    }
}