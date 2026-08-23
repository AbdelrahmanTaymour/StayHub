using FluentAssertions;
using NSubstitute;
using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Users.LogInUser;
using StayHub.Domain.Abstractions;

namespace StayHub.Application.UnitTests.Users;

public class LogInUserTests
{
    private readonly LogInUser _handler;
    private readonly IJwtService _jwtServiceMock = Substitute.For<IJwtService>();

    public LogInUserTests()
    {
        _handler = new LogInUser(_jwtServiceMock);
    }

    [Fact]
    public async Task Handle_Should_DelegateToJwtService_AndReturnItsResult()
    {
        // Arrange
        var command = new LogInUserCommand(Email: "test@test.com", Password: "P@ssw0rd!");
        var expectedResponse = new AccessTokenResponse("access-token", "refresh-token", 300);
        _jwtServiceMock.GetAccessTokenAsync(command.Email, command.Password, Arg.Any<CancellationToken>())
            .Returns(Result.Success(expectedResponse));

        // Act
        var result = await _handler.Handle(command, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(expectedResponse);
        await _jwtServiceMock.Received(1)
            .GetAccessTokenAsync(command.Email, command.Password, Arg.Any<CancellationToken>());
    }
}