using StayHub.Domain.Abstractions;
using StayHub.Domain.Conversations.Events;

namespace StayHub.Domain.Conversations;

public sealed class Conversation : Entity
{
    private Conversation(
        Guid id,
        Guid initiatorId,
        Guid recipientId,
        Guid? apartmentId,
        Guid? bookingId,
        DateTime createdOnUtc)
        : base(id)
    {
        InitiatorId = initiatorId;
        RecipientId = recipientId;
        ApartmentId = apartmentId;
        BookingId = bookingId;
        CreatedOnUtc = createdOnUtc;
    }

    private Conversation()
    {
    }

    public Guid InitiatorId { get; private set; }
    public Guid RecipientId { get; private set; }
    public Guid? ApartmentId { get; private set; }
    public Guid? BookingId { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime? LastMessageOnUtc { get; private set; }

    public static Result<Conversation> Start(
        Guid initiatorId,
        Guid recipientId,
        Guid? apartmentId,
        Guid? bookingId,
        DateTime utcNow)
    {
        if (initiatorId == recipientId)
        {
            return Result.Failure<Conversation>(ConversationErrors.CannotMessageSelf);
        }

        var conversation = new Conversation(
            Guid.CreateVersion7(),
            initiatorId,
            recipientId,
            apartmentId,
            bookingId,
            utcNow);

        conversation.RaiseDomainEvent(new ConversationStartedDomainEvent(conversation.Id));

        return conversation;
    }

    /// <summary>Set once; a conversation already tied to an apartment keeps its original one.</summary>
    public void AttachApartment(Guid apartmentId)
    {
        if (ApartmentId is not null)
        {
            return;
        }

        ApartmentId = apartmentId;
    }

    public void AttachBooking(Guid bookingId)
    {
        if (BookingId is not null)
        {
            return;
        }

        BookingId = bookingId;
    }

    public void RegisterMessage(DateTime sentOnUtc)
    {
        LastMessageOnUtc = sentOnUtc;
    }

    public void RegisterRead(Guid readerId, DateTime readOnUtc)
    {
        RaiseDomainEvent(new ConversationReadDomainEvent(Id, readerId, readOnUtc));
    }
}