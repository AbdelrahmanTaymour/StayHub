using StayHub.Domain.Abstractions;
using StayHub.Domain.Apartments.Events;

namespace StayHub.Domain.Apartments;

public sealed class ApartmentAvailabilityBlock : Entity
{
    private ApartmentAvailabilityBlock(
        Guid id,
        Guid apartmentId,
        DateOnly start,
        DateOnly end,
        ApartmentUnavailabilityReason reason,
        DateTime createdOnUtc) : base(id)
    {
        ApartmentId = apartmentId;
        Start = start;
        End = end;
        Reason = reason;
        CreatedOnUtc = createdOnUtc;
    }

    public Guid ApartmentId { get; }
    public DateOnly Start { get; }
    public DateOnly End { get; }
    public ApartmentUnavailabilityReason Reason { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }

    public static ApartmentAvailabilityBlock Create(
        Guid apartmentId,
        DateOnly start,
        DateOnly end,
        ApartmentUnavailabilityReason reason)
    {
        if (start > end) throw new ApplicationException("End date precedes start date");

        var block = new ApartmentAvailabilityBlock(
            Guid.CreateVersion7(),
            apartmentId,
            start,
            end,
            reason,
            DateTime.UtcNow);

        block.RaiseDomainEvent(
            new ApartmentAvailabilityBlockCreatedDomainEvent(block.Id, block.ApartmentId, block.Start, block.End));

        return block;
    }

    public bool Overlaps(DateOnly otherStart, DateOnly otherEnd)
    {
        return Start <= otherEnd && otherStart <= End;
    }
}