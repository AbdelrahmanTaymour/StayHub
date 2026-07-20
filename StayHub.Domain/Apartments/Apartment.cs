using StayHub.Domain.Abstractions;
using StayHub.Domain.Apartments.Events;
using StayHub.Domain.Shared;

namespace StayHub.Domain.Apartments;

public sealed class Apartment : Entity
{
    private Apartment(Guid id,
        Guid ownerId,
        Name name,
        Description description,
        Address address,
        Money price,
        Money cleaningFee,
        bool isActive,
        DateTime createdOnUtc) : base(id)
    {
        OwnerId = ownerId;
        Name = name;
        Description = description;
        Address = address;
        Price = price;
        CleaningFee = cleaningFee;
        IsActive = isActive;
        CreatedOnUtc = createdOnUtc;
    }

    public Guid OwnerId { get; private set; }
    public Name Name { get; private set; }
    public Description Description { get; private set; }
    public Address Address { get; private set; }
    public Money Price { get; private set; }
    public Money CleaningFee { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime? LastBookedOnUtc { get; internal set; }
    public List<Amenity> Amenities { get; private set; } = new();

    public static Apartment Create(
        Guid ownerId,
        Name name,
        Description description,
        Address address,
        Money price,
        Money cleaningFee,
        DateTime utcNow)
    {
        var apartment = new Apartment(
            Guid.NewGuid(),
            ownerId,
            name,
            description,
            address,
            price,
            cleaningFee,
            true,
            utcNow);

        apartment.RaiseDomainEvent(new ApartmentCreatedDomainEvent(apartment.Id));

        return apartment;
    }

    public void UpdateDetails(
        Name name,
        Description description,
        Money price,
        Money cleaningFee)
    {
        Name = name;
        Description = description;
        Price = price;
        CleaningFee = cleaningFee;
    }

    public Result AddAmenity(Amenity amenity)
    {
        if (Amenities.Any(a => a == amenity))
        {
            return Result.Failure(ApartmentErrors.AmenityAlreadyAdded);
        }

        Amenities.Add(amenity);

        return Result.Success();
    }

    public Result RemoveAmenity(Amenity amenity)
    {
        var apartmentAmenity = Amenities.FirstOrDefault(a => a == amenity);

        if (Amenities.Any(a => a == amenity))
        {
            return Result.Failure(ApartmentErrors.AmenityNotFound);
        }

        Amenities.Remove(apartmentAmenity);

        return Result.Success();
    }

    public Result Deactivate()
    {
        if (!IsActive)
        {
            return Result.Failure(ApartmentErrors.AlreadyInactive);
        }

        IsActive = false;

        RaiseDomainEvent(new ApartmentDeactivatedDomainEvent(Id));

        return Result.Success();
    }

    public Result Activate()
    {
        if (IsActive)
        {
            return Result.Failure(ApartmentErrors.AlreadyActive);
        }

        IsActive = true;

        return Result.Success();
    }
}