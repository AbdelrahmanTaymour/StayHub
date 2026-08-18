using StayHub.Domain.Apartments;
using StayHub.Domain.Shared;
using StayHub.Domain.UnitTests.Users;

namespace StayHub.Domain.UnitTests.Apartments;

internal static class ApartmentData
{
    public static readonly Name Name = new("Test apartment");
    public static readonly Description Description = new("Test description");
    public static readonly Address Address = Address.Create("Country", "State", "ZipCode", "City", "Street");
    public static readonly Money Price = new(100m, Currency.Usd);
    public static readonly Money CleaningFee = new(20m, Currency.Usd);

    public static Apartment Create(Money? price = null, Money? cleaningFee = null)
    {
        return Apartment.Create(
            UserData.OwnerId,
            Name,
            Description,
            Address,
            price ?? Price,
            cleaningFee ?? CleaningFee,
            DateTime.UtcNow);
    }

    public static ApartmentImage CreateImage(Guid? apartmentId = null, bool isPrimary = false, int displayOrder = 0)
    {
        return ApartmentImage.Create(
            apartmentId ?? Guid.CreateVersion7(),
            new ImageUrl("https://cdn.stayhub.dev/images/test.png"),
            displayOrder,
            DateTime.UtcNow,
            isPrimary);
    }

    public static ApartmentStaffAssignment CreateStaffAssignment(
        Guid? apartmentId = null,
        Guid? userId = null,
        ApartmentStaffRole role = ApartmentStaffRole.Cleaner)
    {
        return ApartmentStaffAssignment.Create(
            apartmentId ?? Guid.CreateVersion7(),
            userId ?? Guid.CreateVersion7(),
            role,
            DateTime.UtcNow);
    }

    public static ApartmentAvailabilityBlock CreateAvailabilityBlock(
        Guid? apartmentId = null,
        DateOnly? start = null,
        DateOnly? end = null,
        ApartmentUnavailabilityReason reason = ApartmentUnavailabilityReason.OwnerBlocked)
    {
        return ApartmentAvailabilityBlock.Create(
            apartmentId ?? Guid.CreateVersion7(),
            start ?? new DateOnly(2026, 1, 1),
            end ?? new DateOnly(2026, 1, 10),
            reason,
            DateTime.UtcNow);
    }
}