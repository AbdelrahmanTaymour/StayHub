using StayHub.Domain.Apartments;
using StayHub.Domain.Shared;

namespace StayHub.Application.UnitTests.Apartments;

internal static class ApartmentData
{
    public static Apartment Create() => Apartment.Create(
        Guid.CreateVersion7(),
        new Name("Test apartment"),
        new Description("Test description"),
        Address.Create("Street", "City", "State", "ZipCode", "Country"),
        new Money(100.0m, Currency.Usd),
        Money.Zero(),
        DateTime.UtcNow);
}