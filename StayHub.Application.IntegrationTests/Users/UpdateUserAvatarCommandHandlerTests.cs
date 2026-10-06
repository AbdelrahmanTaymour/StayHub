using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using StayHub.Application.IntegrationTests.Integration;
using StayHub.Application.Users.UpdateUserAvatar;
using StayHub.Domain.Users;

namespace StayHub.Application.IntegrationTests.Users;

public class UpdateUserAvatarCommandHandlerTests(IntegrationTestWebAppFactory factory)
    : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task UpdateUserAvatar_ShouldUploadAvatarAndPersistProfile_ViaOutboxPipeline()
    {
        // Arrange
        var user = UserTestData.CreateUser();
        var profile = UserTestData.CreateProfile(user.Id);

        DbContext.AddRange(user, profile);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(user.Id, Role.Guest.Name);

        var command = new UpdateUserAvatarCommand(
            new MemoryStream([1, 2, 3]), "avatar.png", "image/png");

        // Act
        var result = await Sender.Send(command);

        // Assert
        result.IsSuccess.Should().BeTrue();

        FileStorageService.UploadedFiles.Should().ContainSingle();

        var uploadedFile = FileStorageService.UploadedFiles.Single();
        uploadedFile.FileName.Should().Be("avatar.png");
        uploadedFile.ContentType.Should().Be("image/png");

        DbContext.ChangeTracker.Clear();

        var persistedProfile = await DbContext
            .Set<UserProfile>()
            .SingleAsync(p => p.UserId == user.Id);

        persistedProfile.AvatarKey.Should().NotBeNull();
        persistedProfile.AvatarKey.key.Should().Be(uploadedFile.Key);
    }

    [Fact]
    public async Task UpdateUserAvatar_ShouldDeleteOldAvatar_WhenProfileAlreadyHasAvatar()
    {
        // Arrange
        var user = UserTestData.CreateUser();
        var profile = UserTestData.CreateProfile(user.Id);

        const string oldAvatarKey = "users/avatars/old-avatar.png";

        profile.UpdateAvatar(new AvatarKey(oldAvatarKey), DateTime.UtcNow.AddMinutes(-5));

        DbContext.AddRange(user, profile);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(user.Id, Role.Guest.Name);

        var command = new UpdateUserAvatarCommand(
            new MemoryStream([1, 2, 3]), "new-avatar.png", "image/png");

        // Act
        var result = await Sender.Send(command);

        // Assert
        result.IsSuccess.Should().BeTrue();

        FileStorageService.UploadedFiles.Should().ContainSingle();

        var uploadedFile = FileStorageService.UploadedFiles.Single();

        FileStorageService.DeletedUrls.Should().Contain(oldAvatarKey);

        DbContext.ChangeTracker.Clear();

        var persistedProfile = await DbContext
            .Set<UserProfile>()
            .SingleAsync(p => p.UserId == user.Id);

        persistedProfile.AvatarKey.Should().NotBeNull();
        persistedProfile.AvatarKey.key.Should().Be(uploadedFile.Key);
        persistedProfile.AvatarKey.key.Should().NotBe(oldAvatarKey);
    }

    [Fact]
    public async Task UpdateUserAvatar_ShouldDeleteUploadedAvatar_WhenSaveChangesFails()
    {
        // Arrange
        var user = UserTestData.CreateUser();
        var profile = UserTestData.CreateProfile(user.Id);

        DbContext.AddRange(user, profile);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(user.Id, Role.Guest.Name);

        SaveChangesInterceptor.FailNextSave =
            new InvalidOperationException("Database unavailable");

        var command = new UpdateUserAvatarCommand(
            new MemoryStream([1, 2, 3]), "avatar.png", "image/png");

        // Act
        var act = () => Sender.Send(command);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();

        FileStorageService.UploadedFiles.Should().ContainSingle();

        var uploadedFile = FileStorageService.UploadedFiles.Single();

        FileStorageService.DeletedUrls.Should().Contain(uploadedFile.Key);
    }
}