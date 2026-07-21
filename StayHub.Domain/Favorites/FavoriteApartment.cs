using StayHub.Domain.Abstractions;

namespace StayHub.Domain.Favorites;

public sealed class FavoriteApartment : Entity
{
    private FavoriteApartment(
        Guid id,
        Guid userId,
        Guid apartmentId,
        DateTime createdOnUtc)
        : base(id)
    {
        UserId = userId;
        ApartmentId = apartmentId;
        CreatedOnUtc = createdOnUtc;
    }

    public Guid UserId { get; private set; }

    public Guid ApartmentId { get; private set; }

    public DateTime CreatedOnUtc { get; private set; }

    public static FavoriteApartment Create(Guid userId, Guid apartmentId, DateTime createdOnUtc)
    {
        return new FavoriteApartment(Guid.CreateVersion7(), userId, apartmentId, createdOnUtc);
    }
}