using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using StayHub.Application.Abstractions.Caching;
using StayHub.Application.IntegrationTests.Integration;
using StayHub.Application.Users.UpdateUserProfile;
using StayHub.Domain.Users;

namespace StayHub.Application.IntegrationTests.Users;

public class UpdateUserProfileTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task UpdateUserProfile_ShouldInvalidateBothCacheKeys_ViaOutboxPipeline()
    {
        // Arrange
        var user = UserTestData.CreateUser();
        var profile = UserTestData.CreateProfile(user.Id);
        DbContext.AddRange(user, profile);
        await DbContext.SaveChangesAsync();

        var userCacheKey = CacheKeys.User(user.Id);
        var loggedInUserCacheKey = CacheKeys.LoggedInUser(user.Id);
        await CacheService.SetAsync(userCacheKey, "cached-user");
        await CacheService.SetAsync(loggedInUserCacheKey, "cached-logged-in-user");

        SetCurrentUser(user.Id, Role.Guest.Name);

        var command = new UpdateUserProfileCommand(user.Id, null, "Updated bio for testing.", "+15551234567");

        // Act
        var result = await Sender.Send(command);
        result.IsSuccess.Should().BeTrue();

        await ProcessOutboxAsync();

        // Assert
        (await CacheService.GetAsync<string>(userCacheKey)).Should().BeNull();
        (await CacheService.GetAsync<string>(loggedInUserCacheKey)).Should().BeNull();

        DbContext.ChangeTracker.Clear();
        var persistedProfile = await DbContext.Set<UserProfile>().SingleAsync(p => p.UserId == user.Id);
        persistedProfile.Bio!.Value.Should().Be("Updated bio for testing.");
        persistedProfile.PhoneNumber!.Value.Should().Be("+15551234567");
    }
}