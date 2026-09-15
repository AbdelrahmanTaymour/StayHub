using System.Text.Json;
using MediatR;
using StayHub.Application.Abstractions.Clock;
using StayHub.Application.Abstractions.Email;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Apartments;
using StayHub.Domain.Maintenance;
using StayHub.Domain.Maintenance.Events;
using StayHub.Domain.Notifications;
using StayHub.Domain.Users;

namespace StayHub.Application.Maintenance.CreateMaintenanceRequest;

public class MaintenanceRequestCreatedDomainEventHandler(
    IMaintenanceRequestRepository maintenanceRequestRepository,
    IApartmentRepository apartmentRepository,
    IApartmentStaffAssignmentRepository staffAssignmentRepository,
    IUserRepository userRepository,
    INotificationRepository notificationRepository,
    IUnitOfWork unitOfWork,
    IEmailService emailService,
    IDateTimeProvider dateTimeProvider) : INotificationHandler<MaintenanceRequestCreatedDomainEvent>
{
    public async Task Handle(MaintenanceRequestCreatedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var maintenanceRequest = await maintenanceRequestRepository.GetByIdAsync(
            domainEvent.MaintenanceRequestId,
            cancellationToken);

        if (maintenanceRequest is null) return;

        var apartment = await apartmentRepository.GetByIdAsync(maintenanceRequest.ApartmentId, cancellationToken);

        if (apartment is null) return;

        var recipientIds = new List<Guid> { apartment.OwnerId };

        var activeStaff = await staffAssignmentRepository.GetActiveByApartmentIdAsync(apartment.Id, cancellationToken);

        recipientIds.AddRange(activeStaff.Select(s => s.UserId));

        var payload = new MaintenanceRequestCreatedNotificationPayload(
            MaintenanceRequestId: maintenanceRequest.Id,
            ApartmentId: maintenanceRequest.ApartmentId,
            Comment: maintenanceRequest.Title.Value);

        var serializedPayload = JsonSerializer.Serialize(payload);

        foreach (var recipientId in recipientIds.Distinct())
        {
            var recipient = await userRepository.GetByIdAsync(recipientId, cancellationToken);

            if (recipient is null) continue;

            var notification = Notification.Create(
                recipientId,
                NotificationType.MaintenanceRequestCreated,
                serializedPayload,
                dateTimeProvider.UtcNow);

            notificationRepository.Add(notification);

            await emailService.SendAsync(
                recipient.Email,
                "New maintenance request",
                $"A new issue has been reported: {maintenanceRequest.Title}");
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}