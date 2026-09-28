using FluentAssertions;
using StayHub.Application.Apartments.GetApartmentImages;
using StayHub.Application.IntegrationTests.Integration;
using StayHub.Application.IntegrationTests.Users;
using StayHub.Domain.Apartments;
using StayHub.Domain.Users;

namespace StayHub.Application.IntegrationTests.Apartments;

public class GetApartmentImagesTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task GetImages_ShouldReturnNotFound_WhenApartmentDoesNotExist()
    {
        // Arrange
        SetCurrentUser(Guid.CreateVersion7(), Role.Guest.Name);

        // Act
        var result = await Sender.Send(new GetApartmentImagesQuery(Guid.CreateVersion7()));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ApartmentErrors.NotFound);
    }

    [Fact]
    public async Task GetImages_ShouldReturnNotFound_WhenCallerIsNotTheOwnerOrAdmin()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);
        DbContext.AddRange(owner, apartment);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(Guid.CreateVersion7(), Role.Guest.Name);

        // Act
        var result = await Sender.Send(new GetApartmentImagesQuery(apartment.Id));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ApartmentErrors.NotFound);
    }

    [Fact]
    public async Task GetImages_ShouldReturnEmptyList_WhenApartmentHasNoImages()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);
        DbContext.AddRange(owner, apartment);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(owner.Id, Role.Guest.Name);

        // Act
        var result = await Sender.Send(new GetApartmentImagesQuery(apartment.Id));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Photos.Should().BeEmpty();
    }

    [Fact]
    public async Task GetImages_ShouldReturnImagesInDisplayOrder_WhenCallerIsTheOwner()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);
        DbContext.AddRange(owner, apartment);

        var secondImage = ApartmentTestData.CreateImage(apartment.Id, displayOrder: 1);
        var firstImage = ApartmentTestData.CreateImage(apartment.Id, displayOrder: 0, isPrimary: true);

        DbContext.Add(secondImage);
        DbContext.Add(firstImage);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(owner.Id, Role.Guest.Name);

        // Act
        var result = await Sender.Send(new GetApartmentImagesQuery(apartment.Id));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Photos.Should().HaveCount(2);
        result.Value.Photos[0].Id.Should().Be(firstImage.Id);
        result.Value.Photos[0].IsPrimary.Should().BeTrue();
        result.Value.Photos[1].Id.Should().Be(secondImage.Id);
        result.Value.Photos[1].IsPrimary.Should().BeFalse();
    }

    [Fact]
    public async Task GetImages_ShouldReturnImages_WhenCallerIsAdmin()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);
        var image = ApartmentTestData.CreateImage(apartment.Id, displayOrder: 0, isPrimary: true);
        DbContext.AddRange(owner, apartment, image);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(Guid.CreateVersion7(), Role.Admin.Name);

        // Act
        var result = await Sender.Send(new GetApartmentImagesQuery(apartment.Id));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Photos.Should().ContainSingle(p => p.Id == image.Id);
    }
}