using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using StayHub.Application.Apartments.SetAsPrimaryImage;
using StayHub.Application.IntegrationTests.Integration;
using StayHub.Application.IntegrationTests.Users;
using StayHub.Domain.Apartments;

namespace StayHub.Application.IntegrationTests.Apartments;

public sealed class SetAsPrimaryImageTests(IntegrationTestWebAppFactory factory)
    : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task SetAsPrimaryImage_ShouldPersistNewPrimaryImage()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);

        var currentPrimaryImage = ApartmentImage.Create(
            apartment.Id,
            new ApartmentImageUrl("primary.jpg"),
            displayOrder: 0,
            DateTime.UtcNow.AddMinutes(-2),
            isPrimary: true);

        var newImage = ApartmentImage.Create(
            apartment.Id,
            new ApartmentImageUrl("secondary.jpg"),
            displayOrder: 1,
            DateTime.UtcNow.AddMinutes(-1),
            isPrimary: false);

        DbContext.AddRange(
            owner,
            apartment,
            currentPrimaryImage,
            newImage);

        await DbContext.SaveChangesAsync();

        // Act
        var result = await Sender.Send(
            new SetAsPrimaryImageCommand(
                apartment.Id,
                newImage.Id));

        // Assert
        result.IsSuccess.Should().BeTrue();

        DbContext.ChangeTracker.Clear();

        var images = await DbContext.Set<ApartmentImage>()
            .Where(i => i.ApartmentId == apartment.Id)
            .ToListAsync();

        images.Should().HaveCount(2);

        images.Count(i => i.IsPrimary)
            .Should()
            .Be(1);

        var persistedOldPrimary = images
            .Single(i => i.Id == currentPrimaryImage.Id);

        persistedOldPrimary.IsPrimary.Should().BeFalse();

        var persistedNewPrimary = images
            .Single(i => i.Id == newImage.Id);

        persistedNewPrimary.IsPrimary.Should().BeTrue();
    }

    [Fact]
    public async Task SetAsPrimaryImage_ShouldReturnNotFound_WhenImageBelongsToAnotherApartment()
    {
        // Arrange
        var owner = UserTestData.CreateUser();

        var firstApartment = ApartmentTestData.CreateApartment(
            ownerId: owner.Id);

        var secondApartment = ApartmentTestData.CreateApartment(
            ownerId: owner.Id);

        var image = ApartmentImage.Create(
            firstApartment.Id,
            new ApartmentImageUrl("image.jpg"),
            displayOrder: 0,
            DateTime.UtcNow,
            isPrimary: false);

        DbContext.AddRange(
            owner,
            firstApartment,
            secondApartment,
            image);

        await DbContext.SaveChangesAsync();

        // Act
        var result = await Sender.Send(
            new SetAsPrimaryImageCommand(
                secondApartment.Id,
                image.Id));

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ApartmentImageErrors.NotFound);
    }
}