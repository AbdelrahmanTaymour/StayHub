using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Abstractions.Clock;
using StayHub.Application.Abstractions.Storage;
using StayHub.Application.Users.UpdateUserAvatar;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Users;

namespace StayHub.Application.UnitTests.Users;

public class UpdateUserAvatarCommandHandlerTests
{
    private const string UploadedAvatarKey = "users/avatars/new-avatar.png";
    private const string ExistingAvatarKey = "users/avatars/old-avatar.png";

    private static readonly DateTime UtcNow = DateTime.UtcNow;

    private readonly IDateTimeProvider _dateTimeProviderMock = Substitute.For<IDateTimeProvider>();
    private readonly IFileStorageService _fileStorageServiceMock = Substitute.For<IFileStorageService>();

    private readonly UpdateUserAvatarCommandHandler _handler;
    private readonly IUnitOfWork _unitOfWorkMock = Substitute.For<IUnitOfWork>();
    private readonly IUserContext _userContextMock = Substitute.For<IUserContext>();

    private readonly IUserProfileRepository _userProfileRepositoryMock =
        Substitute.For<IUserProfileRepository>();

    public UpdateUserAvatarCommandHandlerTests()
    {
        _dateTimeProviderMock.UtcNow.Returns(UtcNow);

        _fileStorageServiceMock
            .UploadAsync(
                Arg.Any<Stream>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                ImageCategory.UserAvatar,
                Arg.Any<CancellationToken>())
            .Returns(UploadedAvatarKey);

        _handler = new UpdateUserAvatarCommandHandler(
            _userProfileRepositoryMock,
            _userContextMock,
            _fileStorageServiceMock,
            _unitOfWorkMock,
            _dateTimeProviderMock);
    }

    private static UpdateUserAvatarCommand CommandFor(
        string fileName = "avatar.png",
        string contentType = "image/png") =>
        new(
            FileContent: new MemoryStream([1, 2, 3]),
            FileName: fileName,
            ContentType: contentType);

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenProfileNotFound()
    {
        // Arrange
        var userId = Guid.CreateVersion7();
        _userContextMock.UserId.Returns(userId);

        _userProfileRepositoryMock
            .GetByUserIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns((UserProfile?)null);

        var command = CommandFor();

        // Act
        var result = await _handler.Handle(command, default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserProfileErrors.NotFound);

        await _fileStorageServiceMock.DidNotReceive()
            .UploadAsync(
                Arg.Any<Stream>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                ImageCategory.UserAvatar,
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_UpdateAvatarAndSaveChanges_WhenValid()
    {
        // Arrange
        var userId = Guid.CreateVersion7();
        var profile = UserProfile.Create(userId, UtcNow);

        _userContextMock.UserId.Returns(userId);

        _userProfileRepositoryMock
            .GetByUserIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(profile);

        var command = CommandFor();

        // Act
        var result = await _handler.Handle(command, default);

        // Assert
        result.IsSuccess.Should().BeTrue();

        profile.AvatarKey.Should().Be(new AvatarKey(UploadedAvatarKey));

        await _fileStorageServiceMock.Received(1)
            .UploadAsync(
                Arg.Any<Stream>(),
                "avatar.png",
                "image/png",
                ImageCategory.UserAvatar,
                Arg.Any<CancellationToken>());

        await _unitOfWorkMock.Received(1)
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_DeleteExistingAvatar_WhenProfileAlreadyHasAvatar()
    {
        // Arrange
        var userId = Guid.CreateVersion7();
        var profile = UserProfile.Create(userId, UtcNow);

        profile.UpdateAvatar(
            new AvatarKey(ExistingAvatarKey),
            UtcNow.AddMinutes(-5));

        _userContextMock.UserId.Returns(userId);

        _userProfileRepositoryMock
            .GetByUserIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(profile);

        var command = CommandFor();

        // Act
        var result = await _handler.Handle(command, default);

        // Assert
        result.IsSuccess.Should().BeTrue();

        profile.AvatarKey.Should().Be(new AvatarKey(UploadedAvatarKey));

        await _fileStorageServiceMock.Received(1)
            .DeleteAsync(
                ExistingAvatarKey,
                Arg.Any<CancellationToken>());

        await _unitOfWorkMock.Received(1)
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_NotDeleteExistingAvatar_WhenProfileHasNoAvatar()
    {
        // Arrange
        var userId = Guid.CreateVersion7();
        var profile = UserProfile.Create(userId, UtcNow);

        _userContextMock.UserId.Returns(userId);

        _userProfileRepositoryMock
            .GetByUserIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(profile);

        var command = CommandFor();

        // Act
        var result = await _handler.Handle(command, default);

        // Assert
        result.IsSuccess.Should().BeTrue();

        await _fileStorageServiceMock.DidNotReceive()
            .DeleteAsync(
                Arg.Any<string>(),
                Arg.Any<CancellationToken>());

        await _unitOfWorkMock.Received(1)
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_DeleteUploadedAvatarAndRethrow_WhenSaveChangesFails()
    {
        // Arrange — if the database commit fails after the new avatar
        // has already been uploaded, the handler removes the orphaned file.
        var userId = Guid.CreateVersion7();
        var profile = UserProfile.Create(userId, UtcNow);

        _userContextMock.UserId.Returns(userId);

        _userProfileRepositoryMock
            .GetByUserIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(profile);

        _unitOfWorkMock
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Database unavailable"));

        var command = CommandFor();

        // Act
        var act = () => _handler.Handle(command, default);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();

        await _fileStorageServiceMock.Received(1)
            .DeleteAsync(
                UploadedAvatarKey,
                Arg.Any<CancellationToken>());
    }
}