using MediatR;
using StayHub.Application.Abstractions.Email;
using StayHub.Domain.Apartments;
using StayHub.Domain.Maintenance;
using StayHub.Domain.Maintenance.Events;
using StayHub.Domain.Users;

namespace StayHub.Application.Maintenance.CreateMaintenanceRequest;

public class MaintenanceRequestCreatedDomainEventHandler(
    IMaintenanceRequestRepository maintenanceRequestRepository,
    IApartmentRepository apartmentRepository,
    IApartmentStaffAssignmentRepository staffAssignmentRepository,
    IUserRepository userRepository,
    IEmailService emailService) : INotificationHandler<MaintenanceRequestCreatedDomainEvent>
{
    public async Task Handle(MaintenanceRequestCreatedDomainEvent notification, CancellationToken cancellationToken)
    {
        var maintenanceRequest = await maintenanceRequestRepository.GetByIdAsync(
            notification.MaintenanceRequestId,
            cancellationToken);

        if (maintenanceRequest is null) return;

        var apartment = await apartmentRepository.GetByIdAsync(maintenanceRequest.ApartmentId, cancellationToken);

        if (apartment is null) return;

        var recipientIds = new List<Guid> { apartment.OwnerId };

        var activeStaff = await staffAssignmentRepository.GetActiveByApartmentIdAsync(apartment.Id, cancellationToken);

        recipientIds.AddRange(activeStaff.Select(s => s.UserId));

        foreach (var recipientId in recipientIds.Distinct())
        {
            var recipient = await userRepository.GetByIdAsync(recipientId, cancellationToken);

            if (recipient is null) continue;

            await emailService.SendAsync(
                recipient.Email,
                "New maintenance request",
                $"A new issue has been reported: {maintenanceRequest.Title}");
        }
    }
}