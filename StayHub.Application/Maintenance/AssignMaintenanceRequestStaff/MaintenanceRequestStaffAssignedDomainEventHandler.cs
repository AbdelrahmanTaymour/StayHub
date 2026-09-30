using System.Text.Json;
using MediatR;
using StayHub.Application.Abstractions.Clock;
using StayHub.Application.Abstractions.Email;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Maintenance;
using StayHub.Domain.Maintenance.Events;
using StayHub.Domain.Notifications;
using StayHub.Domain.Users;

namespace StayHub.Application.Maintenance.AssignMaintenanceRequestStaff;

public class MaintenanceRequestStaffAssignedDomainEventHandler(
    IMaintenanceRequestRepository maintenanceRequestRepository,
    IUserRepository userRepository,
    INotificationRepository notificationRepository,
    IUnitOfWork unitOfWork,
    IEmailService emailService,
    IDateTimeProvider dateTimeProvider) : INotificationHandler<MaintenanceRequestStaffAssignedDomainEvent>
{
    public async Task Handle(
        MaintenanceRequestStaffAssignedDomainEvent notification,
        CancellationToken cancellationToken)
    {
        var maintenanceRequest = await maintenanceRequestRepository.GetByIdAsync(
            notification.MaintenanceRequestId,
            cancellationToken);

        if (maintenanceRequest is null) return;

        var assignedStaff = await userRepository.GetByIdAsync(notification.StaffId, cancellationToken);

        if (assignedStaff is null) return;

        const string message = "You've been assigned to a maintenance request.";

        var payload = new MaintenanceRequestStaffAssignedNotificationPayload(
            MaintenanceRequestId: maintenanceRequest.Id,
            Message: message);

        var notificationEntity = Notification.Create(
            assignedStaff.Id,
            NotificationType.MaintenanceRequestUpdate,
            JsonSerializer.Serialize(payload),
            dateTimeProvider.UtcNow);

        notificationRepository.Add(notificationEntity);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        await emailService.SendAsync(
            assignedStaff.Email,
            "You've been assigned a maintenance ticket",
            message);
    }
}