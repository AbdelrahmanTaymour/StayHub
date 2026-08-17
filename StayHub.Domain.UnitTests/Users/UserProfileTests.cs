using FluentAssertions;
using StayHub.Domain.UnitTests.Infrastructure;
using StayHub.Domain.Users;
using StayHub.Domain.Users.Events;

namespace StayHub.Domain.UnitTests.Users;

public class UserProfileTests : BaseTest
{
    [Fact]
    public void Create_Should_SetPropertyValues()
    {
        // Arrange
        var utcNow = DateTime.UtcNow;

        // Act
        var profile = UserProfile.Create(UserData.OwnerId, utcNow);

        // Assert
        profile.UserId.Should().Be(UserData.OwnerId);
        profile.CreatedOnUtc.Should().Be(utcNow);
        profile.UpdatedOnUtc.Should().BeNull();
        profile.AvatarUrl.Should().BeNull();
        profile.Bio.Should().BeNull();
        profile.PhoneNumber.Should().BeNull();
    }


    [Fact]
    public void UpdateAvatar_Should_SetAvatarAndUpdatedOnUtc()
    {
        // Arrange
        var profile = UserProfile.Create(UserData.OwnerId, DateTime.UtcNow);
        var avatar = new Avatar("https://cdn.stayhub.dev/avatars/test.png");
        var utcNow = DateTime.UtcNow;

        // Act
        var result = profile.UpdateAvatar(avatar, utcNow);

        // Assert
        result.IsSuccess.Should().BeTrue();
        profile.AvatarUrl.Should().Be(avatar);
        profile.UpdatedOnUtc.Should().Be(utcNow);
    }

    [Fact]
    public void UpdateAvatar_Should_RaiseUserProfileUpdatedDomainEvent()
    {
        // Arrange
        var profile = UserProfile.Create(UserData.OwnerId, DateTime.UtcNow);

        // Act
        profile.UpdateAvatar(new Avatar("https://cdn.stayhub.dev/avatars/test.png"), DateTime.UtcNow);

        // Assert
        var domainEvent = AssertDomainEventWasPublished<UserProfileUpdatedDomainEvent>(profile);
        domainEvent.UserId.Should().Be(UserData.OwnerId);
    }


    [Fact]
    public void UpdateBio_Should_SetBioAndUpdatedOnUtc()
    {
        // Arrange
        var profile = UserProfile.Create(UserData.OwnerId, DateTime.UtcNow);
        var bio = new Bio("Host of three lovely apartments.");
        var utcNow = DateTime.UtcNow;

        // Act
        var result = profile.UpdateBio(bio, utcNow);

        // Assert
        result.IsSuccess.Should().BeTrue();
        profile.Bio.Should().Be(bio);
        profile.UpdatedOnUtc.Should().Be(utcNow);
    }

    [Fact]
    public void UpdateBio_Should_RaiseUserProfileUpdatedDomainEvent()
    {
        // Arrange
        var profile = UserProfile.Create(UserData.OwnerId, DateTime.UtcNow);

        // Act
        profile.UpdateBio(new Bio("Host bio"), DateTime.UtcNow);

        // Assert
        var domainEvent = AssertDomainEventWasPublished<UserProfileUpdatedDomainEvent>(profile);
        domainEvent.UserId.Should().Be(UserData.OwnerId);
    }


    [Fact]
    public void UpdatePhoneNumber_Should_SetPhoneNumberAndUpdatedOnUtc()
    {
        // Arrange
        var profile = UserProfile.Create(UserData.OwnerId, DateTime.UtcNow);
        var phoneNumber = PhoneNumber.Create("+15551234567").Value;
        var utcNow = DateTime.UtcNow;

        // Act
        var result = profile.UpdatePhoneNumber(phoneNumber, utcNow);

        // Assert
        result.IsSuccess.Should().BeTrue();
        profile.PhoneNumber.Should().Be(phoneNumber);
        profile.UpdatedOnUtc.Should().Be(utcNow);
    }

    [Fact]
    public void UpdatePhoneNumber_Should_RaiseUserProfileUpdatedDomainEvent()
    {
        // Arrange
        var profile = UserProfile.Create(UserData.OwnerId, DateTime.UtcNow);
        var phoneNumber = PhoneNumber.Create("+15551234567").Value;

        // Act
        profile.UpdatePhoneNumber(phoneNumber, DateTime.UtcNow);

        // Assert
        var domainEvent = AssertDomainEventWasPublished<UserProfileUpdatedDomainEvent>(profile);
        domainEvent.UserId.Should().Be(UserData.OwnerId);
    }
}