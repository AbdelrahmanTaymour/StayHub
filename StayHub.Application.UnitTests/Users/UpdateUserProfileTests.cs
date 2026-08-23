using FluentAssertions;
using NSubstitute;
using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Abstractions.Clock;
using StayHub.Application.Users.UpdateUserProfile;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Users;

namespace StayHub.Application.UnitTests.Users;

public class UpdateUserProfileTests
{
    private static readonly DateTime UtcNow = DateTime.UtcNow;
    private readonly IDateTimeProvider _dateTimeProviderMock = Substitute.For<IDateTimeProvider>();

    private readonly UpdateUserProfile _handler;
    private readonly IUnitOfWork _unitOfWorkMock = Substitute.For<IUnitOfWork>();
    private readonly IUserContext _userContextMock = Substitute.For<IUserContext>();

    private readonly IUserProfileRepository _userProfileRepositoryMock = Substitute.For<IUserProfileRepository>();

    public UpdateUserProfileTests()
    {
        _dateTimeProviderMock.UtcNow.Returns(UtcNow);

        _handler = new UpdateUserProfile(
            _userProfileRepositoryMock,
            _userContextMock,
            _unitOfWorkMock,
            _dateTimeProviderMock);
    }

    private static UpdateUserProfileCommand CommandFor(Guid userId, string? avatarUrl = null, string? bio = null,
        string? phoneNumber = null) =>
        new(UserId: userId, AvatarUrl: avatarUrl, Bio: bio, PhoneNumber: phoneNumber);

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenCallerIsNotSelfOrAdmin()
    {
        // Arrange
        var targetUserId = Guid.CreateVersion7();
        _userContextMock.UserId.Returns(Guid.CreateVersion7());
        _userContextMock.Roles.Returns([]);
        var command = CommandFor(targetUserId, bio: "New bio");

        // Act
        var result = await _handler.Handle(command, default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserErrors.NotAuthorized);
        await _userProfileRepositoryMock.DidNotReceive()
            .GetByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenProfileNotFound()
    {
        // Arrange
        var userId = Guid.CreateVersion7();
        _userContextMock.UserId.Returns(userId);
        _userProfileRepositoryMock.GetByUserIdAsync(userId, Arg.Any<CancellationToken>()).Returns((UserProfile?)null);
        var command = CommandFor(userId, bio: "New bio");

        // Act
        var result = await _handler.Handle(command, default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserProfileErrors.NotFound);
    }

    [Fact]
    public async Task Handle_Should_UpdateAvatarBioAndPhoneNumber_WhenAllProvidedAndCallerIsSelf()
    {
        // Arrange
        var userId = Guid.CreateVersion7();
        var profile = UserProfile.Create(userId, UtcNow);
        _userContextMock.UserId.Returns(userId);
        _userProfileRepositoryMock.GetByUserIdAsync(userId, Arg.Any<CancellationToken>()).Returns(profile);
        var command = CommandFor(userId, avatarUrl: "https://cdn.stayhub.dev/a.png", bio: "New bio",
            phoneNumber: "+15551234567");

        // Act
        var result = await _handler.Handle(command, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        profile.AvatarUrl.Should().Be(new Avatar("https://cdn.stayhub.dev/a.png"));
        profile.Bio.Should().Be(new Bio("New bio"));
        profile.PhoneNumber!.Value.Should().Be("+15551234567");
    }

    [Fact]
    public async Task Handle_Should_ReturnSuccess_WhenCallerIsAdminUpdatingSomeoneElsesProfile()
    {
        // Arrange
        var targetUserId = Guid.CreateVersion7();
        var profile = UserProfile.Create(targetUserId, UtcNow);
        _userContextMock.UserId.Returns(Guid.CreateVersion7());
        _userContextMock.Roles.Returns([Role.Admin.Name]);
        _userProfileRepositoryMock.GetByUserIdAsync(targetUserId, Arg.Any<CancellationToken>()).Returns(profile);
        var command = CommandFor(targetUserId, bio: "Admin edit");

        // Act
        var result = await _handler.Handle(command, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        profile.Bio.Should().Be(new Bio("Admin edit"));
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenPhoneNumberIsInvalid()
    {
        // Arrange
        var userId = Guid.CreateVersion7();
        var profile = UserProfile.Create(userId, UtcNow);
        _userContextMock.UserId.Returns(userId);
        _userProfileRepositoryMock.GetByUserIdAsync(userId, Arg.Any<CancellationToken>()).Returns(profile);
        var command = CommandFor(userId, phoneNumber: "not-a-number");

        // Act
        var result = await _handler.Handle(command, default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_Should_NotSaveChanges_WhenPhoneNumberIsInvalid_EvenIfOtherFieldsWereValid()
    {
        // Arrange — avatar/bio would have applied successfully in-memory,
        // but nothing gets persisted because SaveChangesAsync is only
        // reached after ALL fields validate.
        var userId = Guid.CreateVersion7();
        var profile = UserProfile.Create(userId, UtcNow);
        _userContextMock.UserId.Returns(userId);
        _userProfileRepositoryMock.GetByUserIdAsync(userId, Arg.Any<CancellationToken>()).Returns(profile);
        var command = CommandFor(userId, avatarUrl: "https://cdn.stayhub.dev/a.png", bio: "New bio",
            phoneNumber: "invalid");

        // Act
        await _handler.Handle(command, default);

        // Assert
        await _unitOfWorkMock.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ReturnSuccessAndSaveChanges_WhenNoOptionalFieldsProvided()
    {
        // Arrange 
        var userId = Guid.CreateVersion7();
        var profile = UserProfile.Create(userId, UtcNow);
        _userContextMock.UserId.Returns(userId);
        _userProfileRepositoryMock.GetByUserIdAsync(userId, Arg.Any<CancellationToken>()).Returns(profile);
        var command = CommandFor(userId);

        // Act
        var result = await _handler.Handle(command, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _unitOfWorkMock.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}