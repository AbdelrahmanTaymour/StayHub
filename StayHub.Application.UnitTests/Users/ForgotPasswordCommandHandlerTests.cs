using System.Linq.Expressions;
using FluentAssertions;
using NSubstitute;
using StayHub.Application.Abstractions.BackgroundJobs;
using StayHub.Application.Users.ForgotPassword;
using StayHub.Domain.Users;

namespace StayHub.Application.UnitTests.Users;

public class ForgotPasswordCommandHandlerTests
{
    private readonly IBackgroundJobScheduler _backgroundJobSchedulerMock = Substitute.For<IBackgroundJobScheduler>();

    private readonly ForgotPasswordCommandHandler _handler;
    private readonly IUserRepository _userRepositoryMock = Substitute.For<IUserRepository>();

    public ForgotPasswordCommandHandlerTests()
    {
        _handler = new ForgotPasswordCommandHandler(_userRepositoryMock, _backgroundJobSchedulerMock);
    }

    private static ForgotPasswordCommand ValidCommand() => new("test@test.com");

    [Fact]
    public async Task Handle_ShouldReturnSuccess_WhenUserDoesNotExist()
    {
        // Arrange
        var command = ValidCommand();

        _userRepositoryMock
            .GetByEmailAsync(command.Email, Arg.Any<CancellationToken>())
            .Returns((User?)null);

        // Act
        var result = await _handler.Handle(command, default);

        // Assert
        result.IsSuccess.Should().BeTrue();

        _backgroundJobSchedulerMock
            .DidNotReceive()
            .Enqueue(Arg.Any<Expression<Func<SendPasswordResetEmailJob, Task>>>());
    }

    [Fact]
    public async Task Handle_ShouldReturnSuccess_WhenUserDoesNotHaveIdentityId()
    {
        // Arrange
        var command = ValidCommand();
        var user = UserData.Create();

        _userRepositoryMock
            .GetByEmailAsync(command.Email, Arg.Any<CancellationToken>())
            .Returns(user);

        // Act
        var result = await _handler.Handle(command, default);

        // Assert
        result.IsSuccess.Should().BeTrue();

        _backgroundJobSchedulerMock
            .DidNotReceive()
            .Enqueue(Arg.Any<Expression<Func<SendPasswordResetEmailJob, Task>>>());
    }

    [Fact]
    public async Task Handle_ShouldEnqueuePasswordResetEmailJob_WhenUserHasIdentityId()
    {
        // Arrange
        var command = ValidCommand();
        var user = UserData.Create();

        user.SetIdentityId("keycloak-identity-id");

        _userRepositoryMock
            .GetByEmailAsync(command.Email, Arg.Any<CancellationToken>())
            .Returns(user);

        // Act
        var result = await _handler.Handle(command, default);

        // Assert
        result.IsSuccess.Should().BeTrue();

        _backgroundJobSchedulerMock
            .Received(1)
            .Enqueue(Arg.Any<Expression<Func<SendPasswordResetEmailJob, Task>>>());
    }

    [Fact]
    public async Task Handle_ShouldNotEnqueuePasswordResetEmailJob_WhenIdentityIdIsEmpty()
    {
        // Arrange
        var command = ValidCommand();
        var user = UserData.Create();

        user.SetIdentityId(string.Empty);

        _userRepositoryMock
            .GetByEmailAsync(command.Email, Arg.Any<CancellationToken>())
            .Returns(user);

        // Act
        var result = await _handler.Handle(command, default);

        // Assert
        result.IsSuccess.Should().BeTrue();

        _backgroundJobSchedulerMock
            .DidNotReceive()
            .Enqueue(Arg.Any<Expression<Func<SendPasswordResetEmailJob, Task>>>());
    }
}