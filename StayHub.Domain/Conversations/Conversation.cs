using StayHub.Domain.Abstractions;
using StayHub.Domain.Conversations.Events;

namespace StayHub.Domain.Conversations;

public sealed class Conversation : Entity
{
    private Conversation(
        Guid id,
        Guid apartmentId,
        Guid? bookingId,
        Guid guestId,
        Guid ownerId,
        DateTime createdOnUtc)
        : base(id)
    {
        ApartmentId = apartmentId;
        BookingId = bookingId;
        GuestId = guestId;
        OwnerId = ownerId;
        CreatedOnUtc = createdOnUtc;
    }

    public Guid ApartmentId { get; private set; }
    public Guid? BookingId { get; private set; }
    public Guid GuestId { get; private set; }
    public Guid OwnerId { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime? LastMessageOnUtc { get; private set; }

    public static Conversation Start(Guid apartmentId, Guid? bookingId, Guid guestId, Guid ownerId)
    {
        var conversation = new Conversation(
            Guid.CreateVersion7(),
            apartmentId,
            bookingId,
            guestId,
            ownerId,
            DateTime.UtcNow);

        conversation.RaiseDomainEvent(new ConversationStartedDomainEvent(conversation.Id));

        return conversation;
    }

    public void RegisterMessage(DateTime sentOnUtc)
    {
        LastMessageOnUtc = sentOnUtc;
    }
}