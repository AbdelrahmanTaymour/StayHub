using FluentAssertions;
using NSubstitute;
using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Users.RefreshAccessToken;
using StayHub.Domain.Abstractions;

namespace StayHub.Application.UnitTests.Users;

public class RefreshAccessTokenTests
{
    private readonly RefreshAccessToken _handler;
    private readonly IJwtService _jwtServiceMock = Substitute.For<IJwtService>();

    public RefreshAccessTokenTests()
    {
        _handler = new RefreshAccessToken(_jwtServiceMock);
    }

    [Fact]
    public async Task Handle_Should_DelegateToJwtService_AndReturnItsResult()
    {
        // Arrange
        var command = new RefreshAccessTokenCommand(RefreshToken: "refresh-token");
        var expectedResponse = new AccessTokenResponse("new-access-token", "new-refresh-token", 300);
        _jwtServiceMock.RefreshAccessTokenAsync(command.RefreshToken, Arg.Any<CancellationToken>())
            .Returns(Result.Success(expectedResponse));

        // Act
        var result = await _handler.Handle(command, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(expectedResponse);
        await _jwtServiceMock.Received(1).RefreshAccessTokenAsync(command.RefreshToken, Arg.Any<CancellationToken>());
    }
}