using FluentAssertions;
using NSubstitute;
using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Notifications.MarkNotificationAsRead;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Notifications;

namespace StayHub.Application.UnitTests.Notifications;

public class MarkNotificationAsReadTests
{
    private readonly MarkNotificationAsReadCommandHandler _handler;
    private readonly INotificationRepository _notificationRepositoryMock = Substitute.For<INotificationRepository>();
    private readonly IUnitOfWork _unitOfWorkMock = Substitute.For<IUnitOfWork>();
    private readonly IUserContext _userContextMock = Substitute.For<IUserContext>();

    public MarkNotificationAsReadTests()
    {
        _handler = new MarkNotificationAsReadCommandHandler(
            _notificationRepositoryMock,
            _userContextMock,
            _unitOfWorkMock);
    }

    private static Notification CreateNotification(Guid userId) =>
        Notification.Create(userId, NotificationType.NewMessage, "{}", DateTime.UtcNow);

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenNotificationNotFound()
    {
        // Arrange
        var notificationId = Guid.CreateVersion7();
        _notificationRepositoryMock.GetByIdAsync(notificationId, Arg.Any<CancellationToken>())
            .Returns((Notification?)null);

        // Act
        var result = await _handler.Handle(new MarkNotificationAsReadCommand(notificationId), default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(NotificationErrors.NotFound);
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenCallerDoesNotOwnNotification()
    {
        // Arrange — notice: no admin override here at all.
        // Notifications are strictly self-scoped.
        var notification = CreateNotification(Guid.CreateVersion7());
        _notificationRepositoryMock.GetByIdAsync(notification.Id, Arg.Any<CancellationToken>()).Returns(notification);
        _userContextMock.UserId.Returns(Guid.CreateVersion7());

        // Act
        var result = await _handler.Handle(new MarkNotificationAsReadCommand(notification.Id), default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(NotificationErrors.NotAuthorized);
    }

    [Fact]
    public async Task Handle_Should_MarkAsReadAndSaveChanges_WhenCallerOwnsNotification()
    {
        // Arrange
        var userId = Guid.CreateVersion7();
        var notification = CreateNotification(userId);
        _notificationRepositoryMock.GetByIdAsync(notification.Id, Arg.Any<CancellationToken>()).Returns(notification);
        _userContextMock.UserId.Returns(userId);

        // Act
        var result = await _handler.Handle(new MarkNotificationAsReadCommand(notification.Id), default);

        // Assert
        result.IsSuccess.Should().BeTrue();
        notification.IsRead.Should().BeTrue();
        await _unitOfWorkMock.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Should_ReturnFailure_WhenAlreadyRead()
    {
        // Arrange
        var userId = Guid.CreateVersion7();
        var notification = CreateNotification(userId);
        notification.MarkAsRead();
        _notificationRepositoryMock.GetByIdAsync(notification.Id, Arg.Any<CancellationToken>()).Returns(notification);
        _userContextMock.UserId.Returns(userId);

        // Act
        var result = await _handler.Handle(new MarkNotificationAsReadCommand(notification.Id), default);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(NotificationErrors.AlreadyRead);
        await _unitOfWorkMock.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}