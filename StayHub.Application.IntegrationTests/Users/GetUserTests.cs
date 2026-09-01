using FluentAssertions;
using StayHub.Application.IntegrationTests.Integration;
using StayHub.Application.Users.GetLoggedInUser;
using StayHub.Application.Users.GetUser;
using StayHub.Domain.Users;

namespace StayHub.Application.IntegrationTests.Users;

public class GetUserTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task GetUser_ShouldReturnNotAuthorized_WhenCallerIsNotOwnerOrAdmin()
    {
        // Arrange
        var user = UserTestData.CreateUser();
        DbContext.Add(user);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(Guid.CreateVersion7(), Role.Guest.Name);

        var query = new GetUserQuery(user.Id);

        // Act
        var result = await Sender.Send(query);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserErrors.NotAuthorized);
    }

    [Fact]
    public async Task GetUser_ShouldReturnNotFound_WhenUserDoesNotExist()
    {
        // Arrange — caller must be Admin to get past the auth guard for an
        // id that isn't their own, reaching the real NotFound SQL path.
        SetCurrentUser(Guid.CreateVersion7(), Role.Admin.Name);

        var query = new GetUserQuery(Guid.CreateVersion7());

        // Act
        var result = await Sender.Send(query);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserErrors.NotFound);
    }

    [Fact]
    public async Task GetUser_ShouldReturnDetailsWithProfile_WhenProfileExists()
    {
        // Arrange
        var user = UserTestData.CreateUser(firstName: "Amina", lastName: "Farouk");
        var profile = UserTestData.CreateProfile(user.Id);
        profile.UpdateAvatar(new Avatar("https://test-storage.local/avatar.png"), DateTime.UtcNow);
        profile.UpdateBio(new Bio("Loves long walks on the Nile Corniche."), DateTime.UtcNow);

        DbContext.AddRange(user, profile);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(user.Id, Role.Guest.Name);

        var query = new GetUserQuery(user.Id);

        // Act
        var result = await Sender.Send(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(user.Id);
        result.Value.FirstName.Should().Be("Amina");
        result.Value.LastName.Should().Be("Farouk");
        result.Value.AvatarUrl.Should().Be("https://test-storage.local/avatar.png");
        result.Value.Bio.Should().Be("Loves long walks on the Nile Corniche.");
        result.Value.PhoneNumber.Should().BeNull();
    }

    [Fact]
    public async Task GetUser_ShouldReturnDetailsWithNullProfileFields_WhenNoProfileExists()
    {
        // Arrange — proves the LEFT JOIN doesn't fail/exclude the user when
        // no user_profiles row exists.
        var user = UserTestData.CreateUser();
        DbContext.Add(user);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(user.Id, Role.Guest.Name);

        var query = new GetUserQuery(user.Id);

        // Act
        var result = await Sender.Send(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(user.Id);
        result.Value.AvatarUrl.Should().BeNull();
        result.Value.Bio.Should().BeNull();
        result.Value.PhoneNumber.Should().BeNull();
    }

    [Fact]
    public async Task GetLoggedInUser_ShouldResolveFromUserContext_NotFromClientInput()
    {
        // Arrange
        var loggedInUser = UserTestData.CreateUser(firstName: "LoggedIn", lastName: "User");
        var otherUser = UserTestData.CreateUser(firstName: "Other", lastName: "User");
        DbContext.AddRange(loggedInUser, otherUser);
        await DbContext.SaveChangesAsync();

        SetCurrentUser(loggedInUser.Id, Role.Guest.Name);

        var query = new GetLoggedInUserQuery(UserContext);

        // Act
        var result = await Sender.Send(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(loggedInUser.Id);
    }
}