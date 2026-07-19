using StayHub.Domain.Abstractions;
using StayHub.Domain.Apartments.Events;

namespace StayHub.Domain.Apartments;

public sealed class ApartmentImage : Entity
{
    private ApartmentImage(
        Guid id,
        Guid apartmentId,
        ImageUrl url,
        int displayOrder,
        bool isPrimary,
        DateTime createdOnUtc) : base(id)
    {
        ApartmentId = apartmentId;
        Url = url;
        DisplayOrder = displayOrder;
        IsPrimary = isPrimary;
        CreatedOnUtc = createdOnUtc;
    }

    public Guid ApartmentId { get; }
    public ImageUrl Url { get; }
    public int DisplayOrder { get; private set; }
    public bool IsPrimary { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }

    public static ApartmentImage Create(Guid apartmentId, ImageUrl url, int displayOrder, bool isPrimary = false)
    {
        var image = new ApartmentImage(Guid.NewGuid(), apartmentId, url, displayOrder, isPrimary, DateTime.UtcNow);

        image.RaiseDomainEvent(new ApartmentImageAddedDomainEvent(image.Id, image.ApartmentId));

        return image;
    }

    public void SetAsPrimary()
    {
        IsPrimary = true;
    }

    public void UnsetAsPrimary()
    {
        IsPrimary = false;
    }

    public void Reorder(int displayOrder)
    {
        DisplayOrder = displayOrder;
    }

    public void MarkForRemoval()
    {
        RaiseDomainEvent(new ApartmentImageRemovedDomainEvent(Id, ApartmentId, Url));
    }
}