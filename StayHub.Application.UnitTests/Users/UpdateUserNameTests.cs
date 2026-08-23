using FluentAssertions;
using NSubstitute;
using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Users.UpdateUserName;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Users;

namespace StayHub.Application.UnitTests.Users;

public class UpdateUserNameTests
{
    private readonly UpdateUserName _handler;
    private readonly IUnitOfWork _unitOfWorkMock = Substitute.For<IUnitOfWork>();
    private readonly IUserContext _userContextMock = Substitute.For<IUserContext>();
    private readonly IUserRepository _userRepositoryMock = Substitute.For<IUserRepository>();

    public UpdateUserNameTests()
    {
        _handler = new UpdateUserName(_userRepositoryMock, _userContextMock, _unitOfWorkMock);
    }

    private static UpdateUserNameCommand CommandFor(Guid userId) =>
        new(UserId: userId, FirstName: "Updated", LastName: "Name");

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenCallerIsNotSelfOrAdmin()
    {
        // Arrange
        var targetUserId = Guid.CreateVersion7();
        _userContextMock.UserId.Returns(Guid.CreateVersion7());
        _userContextMock.Roles.Returns([]);

        // Act
        var result = await _handler.Handle(CommandFor(targetUserId), default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserErrors.NotAuthorized);
        await _userRepositoryMock.DidNotReceive().GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenUserNotFound()
    {
        // Arrange
        var userId = Guid.CreateVersion7();
        _userContextMock.UserId.Returns(userId);
        _userRepositoryMock.GetByIdAsync(userId, Arg.Any<CancellationToken>()).Returns((User?)null);

        // Act
        var result = await _handler.Handle(CommandFor(userId), default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserErrors.NotFound);
    }

    [Fact]
    public async Task Handle_Should_UpdateNameAndSaveChanges_WhenCallerIsSelf()
    {
        // Arrange
        var user = UserData.Create();
        _userContextMock.UserId.Returns(user.Id);
        _userRepositoryMock.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        // Act
        var result = await _handler.Handle(CommandFor(user.Id), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.FirstName.Should().Be(new FirstName("Updated"));
        user.LastName.Should().Be(new LastName("Name"));
        await _unitOfWorkMock.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_UpdateName_WhenCallerIsAdminActingOnSomeoneElse()
    {
        // Arrange
        var user = UserData.Create();
        _userContextMock.UserId.Returns(Guid.CreateVersion7());
        _userContextMock.Roles.Returns([Role.Admin.Name]);
        _userRepositoryMock.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        // Act
        var result = await _handler.Handle(CommandFor(user.Id), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        user.FirstName.Should().Be(new FirstName("Updated"));
    }
}