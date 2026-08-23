using FluentAssertions;
using NSubstitute;
using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Abstractions.Clock;
using StayHub.Application.Users.RevokeUserSession;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Users;

namespace StayHub.Application.UnitTests.Users;

public class RevokeUserSessionTests
{
    private static readonly DateTime UtcNow = DateTime.UtcNow;
    private static readonly DeviceInfo DeviceInfo = new("Chrome on macOS");
    private static readonly IpAddress SessionIpAddress = IpAddress.Create("127.0.0.1").Value;
    private readonly IDateTimeProvider _dateTimeProviderMock = Substitute.For<IDateTimeProvider>();

    private readonly RevokeUserSession _handler;
    private readonly IUnitOfWork _unitOfWorkMock = Substitute.For<IUnitOfWork>();
    private readonly IUserContext _userContextMock = Substitute.For<IUserContext>();

    private readonly IUserSessionRepository _userSessionRepositoryMock = Substitute.For<IUserSessionRepository>();

    public RevokeUserSessionTests()
    {
        _dateTimeProviderMock.UtcNow.Returns(UtcNow);

        _handler = new RevokeUserSession(
            _userSessionRepositoryMock,
            _userContextMock,
            _unitOfWorkMock,
            _dateTimeProviderMock);
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenSessionNotFound()
    {
        // Arrange
        var sessionId = Guid.CreateVersion7();
        _userSessionRepositoryMock.GetByIdAsync(sessionId, Arg.Any<CancellationToken>()).Returns((UserSession?)null);

        // Act
        var result = await _handler.Handle(new RevokeUserSessionCommand(sessionId), default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserSessionErrors.NotFound);
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenCallerDoesNotOwnSessionAndIsNotAdmin()
    {
        // Arrange
        var session = UserSession.Create(Guid.CreateVersion7(), DeviceInfo, SessionIpAddress, UtcNow);
        _userSessionRepositoryMock.GetByIdAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        _userContextMock.UserId.Returns(Guid.CreateVersion7());
        _userContextMock.Roles.Returns([]);

        // Act
        var result = await _handler.Handle(new RevokeUserSessionCommand(session.Id), default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserSessionErrors.NotAuthorized);
        await _unitOfWorkMock.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_RevokeAndSaveChanges_WhenCallerOwnsSession()
    {
        // Arrange
        var userId = Guid.CreateVersion7();
        var session = UserSession.Create(userId, DeviceInfo, SessionIpAddress, UtcNow);
        _userSessionRepositoryMock.GetByIdAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        _userContextMock.UserId.Returns(userId);

        // Act
        var result = await _handler.Handle(new RevokeUserSessionCommand(session.Id), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        session.RevokedOnUtc.Should().Be(UtcNow);
        await _unitOfWorkMock.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_Revoke_WhenCallerIsAdminActingOnSomeoneElsesSession()
    {
        // Arrange
        var session = UserSession.Create(Guid.CreateVersion7(), DeviceInfo, SessionIpAddress, UtcNow);
        _userSessionRepositoryMock.GetByIdAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        _userContextMock.UserId.Returns(Guid.CreateVersion7());
        _userContextMock.Roles.Returns([Role.Admin.Name]);

        // Act
        var result = await _handler.Handle(new RevokeUserSessionCommand(session.Id), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        session.RevokedOnUtc.Should().Be(UtcNow);
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenSessionAlreadyRevoked()
    {
        // Arrange 
        var userId = Guid.CreateVersion7();
        var session = UserSession.Create(userId, DeviceInfo, SessionIpAddress, UtcNow);
        session.Revoke(UtcNow);
        _userSessionRepositoryMock.GetByIdAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        _userContextMock.UserId.Returns(userId);

        // Act
        var result = await _handler.Handle(new RevokeUserSessionCommand(session.Id), default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserSessionErrors.AlreadyRevoked);
        await _unitOfWorkMock.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}