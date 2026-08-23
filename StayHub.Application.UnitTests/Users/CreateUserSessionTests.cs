using FluentAssertions;
using NSubstitute;
using StayHub.Application.Abstractions.Clock;
using StayHub.Application.Users.CreateUserSession;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Users;

namespace StayHub.Application.UnitTests.Users;

public class CreateUserSessionTests
{
    private static readonly DateTime UtcNow = DateTime.UtcNow;
    private readonly IDateTimeProvider _dateTimeProviderMock = Substitute.For<IDateTimeProvider>();

    private readonly CreateUserSession _handler;
    private readonly IUnitOfWork _unitOfWorkMock = Substitute.For<IUnitOfWork>();

    private readonly IUserRepository _userRepositoryMock = Substitute.For<IUserRepository>();
    private readonly IUserSessionRepository _userSessionRepositoryMock = Substitute.For<IUserSessionRepository>();

    public CreateUserSessionTests()
    {
        _dateTimeProviderMock.UtcNow.Returns(UtcNow);

        _handler = new CreateUserSession(
            _userRepositoryMock,
            _userSessionRepositoryMock,
            _unitOfWorkMock,
            _dateTimeProviderMock);
    }

    private static CreateUserSessionCommand ValidCommand(Guid userId) =>
        new(UserId: userId, DeviceInfo: "Chrome on macOS", IpAddress: "127.0.0.1");

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenUserNotFound()
    {
        // Arrange
        var command = ValidCommand(Guid.CreateVersion7());
        _userRepositoryMock.GetByIdAsync(command.UserId, Arg.Any<CancellationToken>()).Returns((User?)null);

        // Act
        var result = await _handler.Handle(command, default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserErrors.NotFound);
        _userSessionRepositoryMock.DidNotReceive().Add(Arg.Any<UserSession>());
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenIpAddressIsInvalid()
    {
        // Arrange
        var user = UserData.Create();
        var command =
            new CreateUserSessionCommand(UserId: user.Id, DeviceInfo: "Chrome on macOS", IpAddress: "not-an-ip");
        _userRepositoryMock.GetByIdAsync(command.UserId, Arg.Any<CancellationToken>()).Returns(user);

        // Act
        var result = await _handler.Handle(command, default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().NotBeNull();
        _userSessionRepositoryMock.DidNotReceive().Add(Arg.Any<UserSession>());
        await _unitOfWorkMock.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ReturnSuccessWithSessionId_WhenValid()
    {
        // Arrange
        var user = UserData.Create();
        var command = ValidCommand(user.Id);
        _userRepositoryMock.GetByIdAsync(command.UserId, Arg.Any<CancellationToken>()).Returns(user);

        // Act
        var result = await _handler.Handle(command, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_Should_AddSessionAndSaveChanges_WhenValid()
    {
        // Arrange
        var user = UserData.Create();
        var command = ValidCommand(user.Id);
        _userRepositoryMock.GetByIdAsync(command.UserId, Arg.Any<CancellationToken>()).Returns(user);

        // Act
        var result = await _handler.Handle(command, default);

        // Assert
        _userSessionRepositoryMock.Received(1).Add(Arg.Is<UserSession>(s =>
            s.Id == result.Value && s.UserId == user.Id));
        await _unitOfWorkMock.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}