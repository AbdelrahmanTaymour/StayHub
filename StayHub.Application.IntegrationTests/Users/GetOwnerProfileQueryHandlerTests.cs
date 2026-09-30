using FluentAssertions;
using StayHub.Application.IntegrationTests.Apartments;
using StayHub.Application.IntegrationTests.Integration;
using StayHub.Application.Users.GetOwnerProfile;
using StayHub.Domain.Users;

namespace StayHub.Application.IntegrationTests.Users;

public class GetOwnerProfileQueryHandlerTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task GetOwnerProfile_ShouldReturnNotFound_WhenUserDoesNotExist()
    {
        // Arrange
        var query = new GetOwnerProfileQuery(Guid.CreateVersion7());

        // Act
        var result = await Sender.Send(query);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserErrors.NotFound);
    }

    [Fact]
    public async Task GetOwnerProfile_ShouldReturnAccessibleToAnonymousCaller()
    {
        // Arrange — no SetCurrentUser call: proves this query has no auth guard.
        var user = UserTestData.CreateUser(firstName: "Amina", lastName: "Farouk");
        DbContext.Add(user);
        await DbContext.SaveChangesAsync();

        var query = new GetOwnerProfileQuery(user.Id);

        // Act
        var result = await Sender.Send(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.FullName.Should().Be("Amina Farouk");
    }

    [Fact]
    public async Task GetOwnerProfile_ShouldReturnNullProfileFields_WhenNoProfileExists()
    {
        // Arrange
        var user = UserTestData.CreateUser();
        DbContext.Add(user);
        await DbContext.SaveChangesAsync();

        var query = new GetOwnerProfileQuery(user.Id);

        // Act
        var result = await Sender.Send(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AvatarUrl.Should().BeNull();
        result.Value.Bio.Should().BeNull();
        result.Value.Rating.Should().BeNull();
        result.Value.ReviewCount.Should().Be(0);
        result.Value.ActiveListingsCount.Should().Be(0);
    }

    [Fact]
    public async Task GetOwnerProfile_ShouldCountOnlyActiveApartments()
    {
        // Arrange
        var owner = UserTestData.CreateUser();
        var active = ApartmentTestData.CreateApartment(ownerId: owner.Id, name: "Active");
        var inactive = ApartmentTestData.CreateApartment(ownerId: owner.Id, name: "Inactive");
        inactive.Deactivate();

        DbContext.AddRange(owner, active, inactive);
        await DbContext.SaveChangesAsync();

        var query = new GetOwnerProfileQuery(owner.Id);

        // Act
        var result = await Sender.Send(query);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ActiveListingsCount.Should().Be(1);
    }
}