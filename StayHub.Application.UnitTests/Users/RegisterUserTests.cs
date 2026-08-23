using FluentAssertions;
using NSubstitute;
using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Abstractions.Clock;
using StayHub.Application.Users.RegisterUser;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Users;

namespace StayHub.Application.UnitTests.Users;

public class RegisterUserTests
{
    private static readonly DateTime UtcNow = DateTime.UtcNow;
    private readonly IAuthenticationService _authenticationServiceMock = Substitute.For<IAuthenticationService>();
    private readonly IDateTimeProvider _dateTimeProviderMock = Substitute.For<IDateTimeProvider>();

    private readonly RegisterUser _handler;
    private readonly IUnitOfWork _unitOfWorkMock = Substitute.For<IUnitOfWork>();
    private readonly IUserProfileRepository _userProfileRepositoryMock = Substitute.For<IUserProfileRepository>();

    private readonly IUserRepository _userRepositoryMock = Substitute.For<IUserRepository>();

    public RegisterUserTests()
    {
        _dateTimeProviderMock.UtcNow.Returns(UtcNow);

        _handler = new RegisterUser(
            _userRepositoryMock,
            _userProfileRepositoryMock,
            _authenticationServiceMock,
            _unitOfWorkMock,
            _dateTimeProviderMock);
    }

    private static RegisterUserCommand ValidCommand() =>
        new(FirstName: "First", LastName: "Last", Email: "test@test.com", Password: "P@ssw0rd!");

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenEmailIsInvalid()
    {
        // Arrange
        var command = ValidCommand() with { Email = "not-an-email" };

        // Act
        var result = await _handler.Handle(command, default);

        // Assert
        result.IsFailure.Should().BeTrue();
        await _userRepositoryMock.DidNotReceive().IsEmailUniqueAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>());
        await _authenticationServiceMock.DidNotReceive()
            .RegisterAsync(Arg.Any<User>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenEmailIsNotUnique()
    {
        // Arrange
        var command = ValidCommand();
        _userRepositoryMock.IsEmailUniqueAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>()).Returns(false);

        // Act
        var result = await _handler.Handle(command, default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserErrors.EmailNotUnique);
        await _authenticationServiceMock.DidNotReceive()
            .RegisterAsync(Arg.Any<User>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenIdentityRegistrationFails()
    {
        // Arrange
        var command = ValidCommand();
        _userRepositoryMock.IsEmailUniqueAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>()).Returns(true);
        _authenticationServiceMock
            .RegisterAsync(Arg.Any<User>(), command.Password, Arg.Any<CancellationToken>())
            .Returns(Result.Failure<string>(AuthenticationErrors.RegistrationFailed));

        // Act
        var result = await _handler.Handle(command, default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(AuthenticationErrors.RegistrationFailed);
        _userRepositoryMock.DidNotReceive().Add(Arg.Any<User>());
        await _unitOfWorkMock.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ReturnSuccessWithUserId_WhenValid()
    {
        // Arrange
        var command = ValidCommand();
        _userRepositoryMock.IsEmailUniqueAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>()).Returns(true);
        _authenticationServiceMock
            .RegisterAsync(Arg.Any<User>(), command.Password, Arg.Any<CancellationToken>())
            .Returns(Result.Success("keycloak-identity-id"));

        // Act
        var result = await _handler.Handle(command, default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_Should_AddUserAndProfileAndSaveChanges_WhenValid()
    {
        // Arrange
        var command = ValidCommand();
        _userRepositoryMock.IsEmailUniqueAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>()).Returns(true);
        _authenticationServiceMock
            .RegisterAsync(Arg.Any<User>(), command.Password, Arg.Any<CancellationToken>())
            .Returns(Result.Success("keycloak-identity-id"));

        // Act
        var result = await _handler.Handle(command, default);

        // Assert
        _userRepositoryMock.Received(1).Add(Arg.Is<User>(u => u.Id == result.Value));
        _userProfileRepositoryMock.Received(1).Add(Arg.Is<UserProfile>(p => p.UserId == result.Value));
        await _unitOfWorkMock.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_SetIdentityIdOnUser_WhenRegistrationSucceeds()
    {
        // Arrange
        var command = ValidCommand();
        _userRepositoryMock.IsEmailUniqueAsync(Arg.Any<Email>(), Arg.Any<CancellationToken>()).Returns(true);
        _authenticationServiceMock
            .RegisterAsync(Arg.Any<User>(), command.Password, Arg.Any<CancellationToken>())
            .Returns(Result.Success("keycloak-identity-id"));

        User? addedUser = null;
        _userRepositoryMock.When(r => r.Add(Arg.Any<User>())).Do(call => addedUser = call.Arg<User>());

        // Act
        await _handler.Handle(command, default);

        // Assert
        addedUser.Should().NotBeNull();
        addedUser!.IdentityId.Should().Be("keycloak-identity-id");
    }
}