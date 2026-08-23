using FluentAssertions;
using NSubstitute;
using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Abstractions.Data;
using StayHub.Application.Users.GetUser;
using StayHub.Domain.Users;

namespace StayHub.Application.UnitTests.Users;

public class GetUserTests
{
    private readonly GetUser _handler;
    private readonly ISqlConnectionFactory _sqlConnectionFactoryMock = Substitute.For<ISqlConnectionFactory>();
    private readonly IUserContext _userContextMock = Substitute.For<IUserContext>();

    public GetUserTests()
    {
        _handler = new GetUser(_sqlConnectionFactoryMock, _userContextMock);
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenCallerIsNotSelfOrAdmin()
    {
        // Arrange
        var targetUserId = Guid.CreateVersion7();
        _userContextMock.UserId.Returns(Guid.CreateVersion7());
        _userContextMock.Roles.Returns([]);

        // Act
        var result = await _handler.Handle(new GetUserQuery(targetUserId), default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserErrors.NotAuthorized);
    }

    [Fact]
    public async Task Handle_Should_NotOpenDatabaseConnection_WhenCallerIsNotSelfOrAdmin()
    {
        // Arrange — confirms the guard genuinely short-circuits before any
        // DB access is attempted, not just that it returns the right error.
        var targetUserId = Guid.CreateVersion7();
        _userContextMock.UserId.Returns(Guid.CreateVersion7());
        _userContextMock.Roles.Returns([]);

        // Act
        await _handler.Handle(new GetUserQuery(targetUserId), default);

        // Assert
        _sqlConnectionFactoryMock.DidNotReceive().CreateConnection();
    }
}