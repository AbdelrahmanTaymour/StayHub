using System.Text.Json;
using MediatR;
using StayHub.Application.Abstractions.Clock;
using StayHub.Application.Abstractions.Email;
using StayHub.Application.Maintenance.CreateMaintenanceRequest;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Maintenance;
using StayHub.Domain.Maintenance.Events;
using StayHub.Domain.Notifications;
using StayHub.Domain.Users;

namespace StayHub.Application.Maintenance.StartMaintenanceRequest;

public class MaintenanceRequestStartedDomainEventHandler(
    IMaintenanceRequestRepository maintenanceRequestRepository,
    IUserRepository userRepository,
    INotificationRepository notificationRepository,
    IUnitOfWork unitOfWork,
    IEmailService emailService,
    IDateTimeProvider dateTimeProvider) : INotificationHandler<MaintenanceRequestStartedDomainEvent>
{
    public async Task Handle(MaintenanceRequestStartedDomainEvent notification, CancellationToken cancellationToken)
    {
        var maintenanceRequest = await maintenanceRequestRepository.GetByIdAsync(
            notification.MaintenanceRequestId,
            cancellationToken);

        if (maintenanceRequest is null) return;

        var maintenanceReporter = await userRepository.GetByIdAsync(notification.ReportedByUserId, cancellationToken);

        if (maintenanceReporter is null) return;

        const string message =
            "A maintenance request has been started for your apartment. Please check the status of the request.";

        var payload = new MaintenanceRequestStartedNotificationPayload(
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
            "Maintenance request started",
            message
        );
    }
}