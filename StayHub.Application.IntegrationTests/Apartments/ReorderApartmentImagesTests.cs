using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using StayHub.Application.Apartments.ReorderApartmentImages;
using StayHub.Application.IntegrationTests.Integration;
using StayHub.Application.IntegrationTests.Users;
using StayHub.Domain.Apartments;
using StayHub.Domain.Users;

namespace StayHub.Application.IntegrationTests.Apartments;

public class ReorderApartmentImagesTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task ReorderApartmentImages_ShouldPersistNewOrder_WhenCallerIsOwner()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);
        DbContext.AddRange(owner, apartment);

        var firstImage = ApartmentTestData.CreateImage(apartment.Id, displayOrder: 0);
        var secondImage = ApartmentTestData.CreateImage(apartment.Id, displayOrder: 1);
        DbContext.AddRange(firstImage, secondImage);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(owner.Id, Role.Guest.Name);

        var command = new ReorderApartmentImagesCommand(apartment.Id, [secondImage.Id, firstImage.Id]);

        // Act
        var result = await Sender.Send(command);

        // Assert
        result.IsSuccess.Should().BeTrue();

        DbContext.ChangeTracker.Clear();
        var persistedSecond = await DbContext.Set<ApartmentImage>().SingleAsync(i => i.Id == secondImage.Id);
        var persistedFirst = await DbContext.Set<ApartmentImage>().SingleAsync(i => i.Id == firstImage.Id);

        persistedSecond.DisplayOrder.Should().Be(0);
        persistedFirst.DisplayOrder.Should().Be(1);
    }

    [Fact]
    public async Task ReorderApartmentImages_ShouldReturnNotAuthorized_WhenCallerIsNotOwnerOrAdmin()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);
        DbContext.Add(owner);
        DbContext.Add(apartment);
        var image = ApartmentTestData.CreateImage(apartment.Id);
        DbContext.Add(image);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(Guid.CreateVersion7(), Role.Guest.Name);

        var command = new ReorderApartmentImagesCommand(apartment.Id, [image.Id]);

        // Act
        var result = await Sender.Send(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ApartmentErrors.NotAuthorized);
    }

    [Fact]
    public async Task ReorderApartmentImages_ShouldReturnInvalidOrderPayload_WhenImageSetDoesNotMatch()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);
        DbContext.AddRange(owner, apartment);
        var image = ApartmentTestData.CreateImage(apartment.Id);
        DbContext.Add(image);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(owner.Id, Role.Guest.Name);

        var command = new ReorderApartmentImagesCommand(apartment.Id, [image.Id, Guid.CreateVersion7()]);

        // Act
        var result = await Sender.Send(command);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ApartmentImageErrors.InvalidOrderPayload);
    }
}