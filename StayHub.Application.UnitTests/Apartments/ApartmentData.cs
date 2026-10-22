using StayHub.Domain.Apartments;
using StayHub.Domain.Shared;

namespace StayHub.Application.UnitTests.Apartments;

internal static class ApartmentData
{
    public static Apartment Create(Guid? id = null) => Apartment.Create(
        id ?? Guid.CreateVersion7(),
        new Name("Test apartment"),
        new Description("Test description"),
        Address.Create("Street", "City", "State", "ZipCode", "Country"),
        new Money(100.0m, Currency.Usd),
        Money.Zero(),
        DateTime.UtcNow);

    public static ApartmentStaffAssignment CreateStaffAssignment(
        Guid? apartmentId = null,
        Guid? userId = null,
        ApartmentStaffRole role = ApartmentStaffRole.Cleaner,
        DateTime? assignedOnUtc = null)
    {
        return ApartmentStaffAssignment.Create(
            apartmentId ?? Guid.CreateVersion7(),
            userId ?? Guid.CreateVersion7(),
            role,
            assignedOnUtc ?? DateTime.UtcNow);
    }

    public static ApartmentImage CreateImage(
        Guid apartmentId,
        bool isPrimary = false) => ApartmentImage.Create(
        apartmentId,
        new ApartmentImageKey("existing-primary.jpg"),
        displayOrder: 0,
        DateTime.UtcNow.AddMinutes(-5),
        isPrimary);
}