using StayHub.Domain.Abstractions;
using StayHub.Domain.Apartments.Events;
using StayHub.Domain.Shared;

namespace StayHub.Domain.Apartments;

public sealed class Apartment : Entity
{
    private readonly List<Amenity> _amenities = [];

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

    private Apartment()
    {
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

    public IReadOnlyCollection<Amenity> Amenities => _amenities.AsReadOnly();

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
            Guid.CreateVersion7(),
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

        RaiseDomainEvent(new ApartmentUpdatedDomainEvent(Id));
    }

    public Result AddAmenity(Amenity amenity)
    {
        if (_amenities.Any(a => a == amenity))
        {
            return Result.Failure(ApartmentErrors.AmenityAlreadyAdded);
        }

        _amenities.Add(amenity);

        RaiseDomainEvent(new ApartmentAmenitiesChangedDomainEvent(Id));

        return Result.Success();
    }

    public Result RemoveAmenity(Amenity amenity)
    {
        var apartmentAmenity = Amenities.FirstOrDefault(a => a == amenity);

        if (_amenities.All(a => a != amenity))
        {
            return Result.Failure(ApartmentErrors.AmenityNotFound);
        }

        _amenities.Remove(apartmentAmenity);

        RaiseDomainEvent(new ApartmentAmenitiesChangedDomainEvent(Id));

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

        RaiseDomainEvent(new ApartmentActivatedDomainEvent(Id));

        return Result.Success();
    }

    public void UpdateLastBooked(DateTime utcNow)
    {
        LastBookedOnUtc = utcNow;
    }
}