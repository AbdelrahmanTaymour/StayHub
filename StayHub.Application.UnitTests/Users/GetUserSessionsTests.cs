using FluentAssertions;
using NSubstitute;
using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Abstractions.Data;
using StayHub.Application.Users.GetUserSessions;
using StayHub.Domain.Users;

namespace StayHub.Application.UnitTests.Users;

public class GetUserSessionsTests
{
    private readonly GetUserSessions _handler;
    private readonly ISqlConnectionFactory _sqlConnectionFactoryMock = Substitute.For<ISqlConnectionFactory>();
    private readonly IUserContext _userContextMock = Substitute.For<IUserContext>();

    public GetUserSessionsTests()
    {
        _handler = new GetUserSessions(_sqlConnectionFactoryMock, _userContextMock);
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenCallerIsNotSelfOrAdmin()
    {
        // Arrange
        var targetUserId = Guid.CreateVersion7();
        _userContextMock.UserId.Returns(Guid.CreateVersion7());
        _userContextMock.Roles.Returns([]);

        // Act
        var result = await _handler.Handle(new GetUserSessionsQuery(targetUserId), default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserSessionErrors.NotAuthorized);
    }

    [Fact]
    public async Task Handle_Should_NotOpenDatabaseConnection_WhenCallerIsNotSelfOrAdmin()
    {
        // Arrange
        var targetUserId = Guid.CreateVersion7();
        _userContextMock.UserId.Returns(Guid.CreateVersion7());
        _userContextMock.Roles.Returns([]);

        // Act
        await _handler.Handle(new GetUserSessionsQuery(targetUserId), default);

        // Assert
        _sqlConnectionFactoryMock.DidNotReceive().CreateConnection();
    }
}