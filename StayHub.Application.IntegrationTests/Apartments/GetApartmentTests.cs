using FluentAssertions;
using StayHub.Application.Apartments.GetApartment;
using StayHub.Application.IntegrationTests.Integration;
using StayHub.Application.IntegrationTests.Users;
using StayHub.Domain.Apartments;

namespace StayHub.Application.IntegrationTests.Apartments;

public class GetApartmentTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task GetApartment_ShouldReturnFailure_WhenApartmentIsNotFound()
    {
        // Arrange
        var query = new GetApartmentQuery(Guid.CreateVersion7());

        // Act
        var result = await Sender.Send(query);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ApartmentErrors.NotFound);
    }

    [Fact]
    public async Task GetApartment_ShouldReturnDetails_WhenApartmentExists()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(
            ownerId: owner.Id,
            name: "Nile View Studio",
            city: "Cairo",
            priceAmount: 750m,
            priceCurrency: "USD");

        var amenity = Enum.GetValues<Amenity>().First();
        apartment.AddAmenity(amenity);

        DbContext.AddRange(owner, apartment);
        await DbContext.SaveChangesAsync();

        var query = new GetApartmentQuery(apartment.Id);

        // Act
        var result = await Sender.Send(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(apartment.Id);
        result.Value.OwnerId.Should().Be(apartment.OwnerId);
        result.Value.Name.Should().Be("Nile View Studio");
        result.Value.PriceAmount.Should().Be(750m);
        result.Value.PriceCurrency.Should().Be("USD");
        result.Value.IsActive.Should().BeTrue();
        result.Value.Address.City.Should().Be("Cairo");
        result.Value.Amenities.Should().ContainSingle().Which.Should().Be(amenity.ToString());
        result.Value.Images.Should().BeEmpty();
    }

    [Fact]
    public async Task GetApartment_ShouldReturnImagesInDisplayOrder_WhenApartmentHasMultipleImages()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);
        DbContext.AddRange(owner, apartment);

        var secondImage = ApartmentTestData.CreateImage(apartment.Id, displayOrder: 1);
        var firstImage = ApartmentTestData.CreateImage(apartment.Id, displayOrder: 0, isPrimary: true);

        // Deliberately added out of order to prove ORDER BY display_order, not insertion order.
        DbContext.Add(secondImage);
        DbContext.Add(firstImage);
        await DbContext.SaveChangesAsync();

        var query = new GetApartmentQuery(apartment.Id);

        // Act
        var result = await Sender.Send(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Images.Should().HaveCount(2);
        result.Value.Images[0].Id.Should().Be(firstImage.Id);
        result.Value.Images[0].IsPrimary.Should().BeTrue();
        result.Value.Images[1].Id.Should().Be(secondImage.Id);
        result.Value.Images[1].IsPrimary.Should().BeFalse();
    }
}