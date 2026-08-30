using StayHub.Domain.Apartments;
using StayHub.Domain.Shared;

namespace StayHub.Application.IntegrationTests.Apartments;

internal static class ApartmentTestData
{
    public static Apartment CreateApartment(
        Guid ownerId,
        string name = "Test Apartment",
        string city = "Cairo",
        decimal priceAmount = 500m,
        string priceCurrency = "USD",
        decimal cleaningFeeAmount = 50m,
        DateTime? utcNow = null)
    {
        var address = Address.Create("123 Test St", city, "Cairo Governorate", "11511", "Egypt");
        var price = new Money(priceAmount, Currency.FromCode(priceCurrency));
        var cleaningFee = new Money(cleaningFeeAmount, Currency.FromCode(priceCurrency));

        return Apartment.Create(
            ownerId,
            new Name(name),
            new Description("A place to stay for testing."),
            address,
            price,
            cleaningFee,
            utcNow ?? DateTime.UtcNow);
    }

    public static ApartmentImage CreateImage(
        Guid apartmentId,
        int displayOrder = 0,
        bool isPrimary = false,
        DateTime? utcNow = null)
    {
        var url = new ImageUrl($"https://test-storage.local/{Guid.NewGuid():N}.jpg");

        return ApartmentImage.Create(apartmentId, url, displayOrder, utcNow ?? DateTime.UtcNow, isPrimary);
    }
}