using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Abstractions.Messaging;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Notifications;

namespace StayHub.Application.Notifications.MarkNotificationAsRead;

internal sealed class MarkNotificationAsReadCommandHandler(
    INotificationRepository notificationRepository,
    IUserContext userContext,
    IUnitOfWork unitOfWork) : ICommandHandler<MarkNotificationAsReadCommand>
{
    public async Task<Result> Handle(MarkNotificationAsReadCommand request, CancellationToken cancellationToken)
    {
        var notification = await notificationRepository.GetByIdAsync(request.NotificationId, cancellationToken);

        if (notification is null) return Result.Failure(NotificationErrors.NotFound);

        if (notification.UserId != userContext.UserId)
            return Result.Failure(NotificationErrors.NotAuthorized);

        var result = notification.MarkAsRead();

        if (result.IsFailure) return result;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}