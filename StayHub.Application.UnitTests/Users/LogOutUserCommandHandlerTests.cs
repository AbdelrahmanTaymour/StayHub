using FluentAssertions;
using NSubstitute;
using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Users.LogOutUser;
using StayHub.Domain.Abstractions;

namespace StayHub.Application.UnitTests.Users;

public class LogOutUserCommandHandlerTests
{
    private readonly LogOutUserCommandHandler _handler;
    private readonly IJwtService _jwtServiceMock = Substitute.For<IJwtService>();

    public LogOutUserCommandHandlerTests()
    {
        _handler = new LogOutUserCommandHandler(_jwtServiceMock);
    }

    [Fact]
    public async Task Handle_Should_DelegateToJwtService_AndReturnItsResult()
    {
        // Arrange
        var command = new LogOutUserCommand(RefreshToken: "refresh-token");
        _jwtServiceMock.LogOutAsync(command.RefreshToken, Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        // Act
        var result = await _handler.Handle(command, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _jwtServiceMock.Received(1).LogOutAsync(command.RefreshToken, Arg.Any<CancellationToken>());
    }
}