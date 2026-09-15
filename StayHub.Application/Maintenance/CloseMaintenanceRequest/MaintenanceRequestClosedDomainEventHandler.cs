using System.Text.Json;
using MediatR;
using StayHub.Application.Abstractions.Clock;
using StayHub.Application.Abstractions.Email;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Maintenance;
using StayHub.Domain.Maintenance.Events;
using StayHub.Domain.Notifications;
using StayHub.Domain.Users;

namespace StayHub.Application.Maintenance.CloseMaintenanceRequest;

public class MaintenanceRequestStartedDomainEventHandler(
    IMaintenanceRequestRepository maintenanceRequestRepository,
    IUserRepository userRepository,
    INotificationRepository notificationRepository,
    IUnitOfWork unitOfWork,
    IEmailService emailService,
    IDateTimeProvider dateTimeProvider) : INotificationHandler<MaintenanceRequestClosedDomainEvent>
{
    public async Task Handle(MaintenanceRequestClosedDomainEvent notification, CancellationToken cancellationToken)
    {
        var maintenanceRequest = await maintenanceRequestRepository.GetByIdAsync(
            notification.MaintenanceRequestId,
            cancellationToken);

        if (maintenanceRequest is null) return;

        var maintenanceReporter = await userRepository.GetByIdAsync(notification.ReportedByUserId, cancellationToken);

        if (maintenanceReporter is null) return;

        const string message =
            "Your maintenance request has been closed. Please check the request for more details.";

        var payload = new MaintenanceRequestClosedNotificationPayload(
            MaintenanceRequestId: maintenanceRequest.Id,
            Message: message);

        var notificationEntity = Notification.Create(
            maintenanceReporter.Id,
            NotificationType.MaintenanceRequestUpdate,
            JsonSerializer.Serialize(payload),
            dateTimeProvider.UtcNow);

        notificationRepository.Add(notificationEntity);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        await emailService.SendAsync(
            maintenanceReporter.Email,
            "Maintenance request closed",
            message);
    }
}