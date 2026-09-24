using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using StayHub.Application.Apartments.AddApartmentImage;
using StayHub.Application.IntegrationTests.Integration;
using StayHub.Application.IntegrationTests.Users;
using StayHub.Domain.Apartments;
using StayHub.Domain.Users;

namespace StayHub.Application.IntegrationTests.Apartments;

public class AddApartmentImageTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task AddApartmentImage_ShouldPersistImage_WhenCallerIsOwner()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);
        DbContext.AddRange(owner, apartment);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(owner.Id, Role.Guest.Name);

        var command = new AddApartmentImageCommand(
            apartment.Id,
            new MemoryStream([1, 2, 3]),
            "photo.jpg",
            "image/jpeg",
            IsPrimary: true);

        // Act
        var result = await Sender.Send(command);

        // Assert
        result.IsSuccess.Should().BeTrue();

        FileStorageService.UploadedFiles.Should().ContainSingle(f => f.FileName == "photo.jpg");

        DbContext.ChangeTracker.Clear();
        var persistedImage = await DbContext.Set<ApartmentImage>().SingleAsync(i => i.Id == result.Value);
        persistedImage.ApartmentId.Should().Be(apartment.Id);
        persistedImage.IsPrimary.Should().BeTrue();
    }

    [Fact]
    public async Task AddApartmentImage_ShouldUnsetExistingPrimary_WhenAddingNewPrimaryImage()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);

        var existingPrimaryImage = ApartmentImage.Create(
            apartment.Id,
            new ApartmentImageUrl("existing-primary.jpg"),
            displayOrder: 0,
            DateTime.UtcNow.AddMinutes(-5),
            isPrimary: true);

        DbContext.AddRange(owner, apartment, existingPrimaryImage);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(owner.Id, Role.Guest.Name);

        var command = new AddApartmentImageCommand(
            apartment.Id,
            new MemoryStream([1, 2, 3]),
            "new-primary.jpg",
            "image/jpeg",
            IsPrimary: true);

        // Act
        var result = await Sender.Send(command);

        // Assert
        result.IsSuccess.Should().BeTrue();

        DbContext.ChangeTracker.Clear();

        var images = await DbContext.Set<ApartmentImage>()
            .Where(i => i.ApartmentId == apartment.Id)
            .OrderBy(i => i.DisplayOrder)
            .ToListAsync();

        images.Should().HaveCount(2);

        images.Count(i => i.IsPrimary).Should().Be(1);

        var persistedOldImage = images.Single(i => i.Id == existingPrimaryImage.Id);
        persistedOldImage.IsPrimary.Should().BeFalse();

        var persistedNewImage = images.Single(i => i.Id == result.Value);
        persistedNewImage.IsPrimary.Should().BeTrue();
    }

    [Fact]
    public async Task AddApartmentImage_ShouldDeleteUploadedBlob_WhenDatabaseSaveFails()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var apartment = ApartmentTestData.CreateApartment(ownerId: owner.Id);
        DbContext.AddRange(owner, apartment);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(owner.Id, Role.Guest.Name);

        var command = new AddApartmentImageCommand(
            apartment.Id,
            new MemoryStream([1, 2, 3]),
            "photo.jpg",
            "image/jpeg",
            IsPrimary: false);

        SaveChangesInterceptor.FailNextSave = new InvalidOperationException("Simulated database failure.");

        // Act
        var act = async () => await Sender.Send(command);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();

        FileStorageService.UploadedFiles.Should().ContainSingle();
        var uploadedUrl = FileStorageService.UploadedFiles.Single().Url;
        FileStorageService.DeletedUrls.Should().ContainSingle(url => url == uploadedUrl);
    }
}