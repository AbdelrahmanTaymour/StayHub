using FluentAssertions;
using StayHub.Domain.UnitTests.Infrastructure;
using StayHub.Domain.Users;
using StayHub.Domain.Users.Events;

namespace StayHub.Domain.UnitTests.Users;

public class UserTests : BaseTest
{
    [Fact]
    public void Create_Should_SetPropertyValues()
    {
        var utcNow = DateTime.UtcNow;

        var user = User.Create(
            UserData.FirstName,
            UserData.LastName,
            UserData.Email,
            utcNow);

        user.Id.Should().NotBeEmpty();
        user.FirstName.Should().Be(UserData.FirstName);
        user.LastName.Should().Be(UserData.LastName);
        user.Email.Should().Be(UserData.Email);
        user.CreatedOnUtc.Should().Be(utcNow);
    }


    [Fact]
    public void Create_Should_RaiseUserCreatedDomainEvent()
    {
        // Act
        var user = UserData.CreateUser();

        // Assert
        var domainEvent = AssertDomainEventWasPublished<UserCreatedDomainEvent>(user);
        domainEvent.UserId.Should().Be(user.Id);
    }

    [Fact]
    public void Create_Should_AssignExactlyOneGuestRole()
    {
        // Act
        var user = UserData.CreateUser();

        // Assert
        user.Roles.Should().HaveCount(1);
        user.Roles.Single().Should().Match<UserRole>(role => role.RoleId == Role.Guest.Id && role.UserId == user.Id);
    }

    [Fact]
    public void UpdateName_Should_SetPropertyValues()
    {
        // Arrange
        var user = UserData.CreateUser();
        var newFirstName = new FirstName("Updated");
        var newLastName = new LastName("Name");

        // Act
        user.UpdateName(newFirstName, newLastName);

        // Assert
        user.FirstName.Should().Be(newFirstName);
        user.LastName.Should().Be(newLastName);
    }

    [Fact]
    public void UpdateName_Should_RaiseUserNameUpdatedDomainEvent()
    {
        // Arrange
        var user = UserData.CreateUser();

        // Act
        user.UpdateName(new FirstName("Updated"), new LastName("Name"));

        // Assert
        var domainEvent = AssertDomainEventWasPublished<UserNameUpdatedDomainEvent>(user);
        domainEvent.UserId.Should().Be(user.Id);
    }

    [Fact]
    public void UpdateName_Should_RaiseDomainEventEachTime_WhenCalledMultipleTimes()
    {
        // Arrange
        var user = UserData.CreateUser();

        // Act
        user.UpdateName(new FirstName("First-Update"), new LastName("Last-Update"));
        user.UpdateName(new FirstName("Second-Update"), new LastName("Last-Update"));

        // Assert — two calls, two events; nothing dedupes or overwrites the
        // first one just because the same event type fires again.
        AssertDomainEventWasPublishedTimes<UserNameUpdatedDomainEvent>(user, 2);
    }


    [Fact]
    public void SetIdentityId_Should_SetIdentityId()
    {
        // Arrange
        var user = UserData.CreateUser();
        const string identityId = "keycloak-identity-id";

        // Act
        user.SetIdentityId(identityId);

        // Assert
        user.IdentityId.Should().Be(identityId);
    }
}