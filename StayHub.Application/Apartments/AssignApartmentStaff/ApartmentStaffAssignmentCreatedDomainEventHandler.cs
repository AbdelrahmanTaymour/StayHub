using System.Text.Json;
using MediatR;
using StayHub.Application.Abstractions.Clock;
using StayHub.Application.Abstractions.Email;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Apartments;
using StayHub.Domain.Apartments.Events;
using StayHub.Domain.Notifications;
using StayHub.Domain.Users;

namespace StayHub.Application.Apartments.AssignApartmentStaff;

public class ApartmentStaffAssignmentCreatedDomainEventHandler(
    IApartmentRepository apartmentRepository,
    IApartmentStaffAssignmentRepository assignmentRepository,
    IUserRepository userRepository,
    INotificationRepository notificationRepository,
    IUnitOfWork unitOfWork,
    IEmailService emailService,
    IDateTimeProvider dateTimeProvider)
    : INotificationHandler<ApartmentStaffAssignmentCreatedDomainEvent>
{
    public async Task Handle(ApartmentStaffAssignmentCreatedDomainEvent notification,
        CancellationToken cancellationToken)
    {
        var apartment = await apartmentRepository.GetByIdAsync(notification.Id, cancellationToken);

        if (apartment is null) return;

        var user = await userRepository.GetByIdAsync(notification.UserId, cancellationToken);

        if (user is null) return;

        var message =
            $"You have been assigned as maintenance staff to the apartment \"{apartment.Name}\"";

        var payload = new ApartmentStaffAssignmentCreatedNotificationPayload(
            AssignmentId: notification.Id,
            ApartmentId: apartment.Id,
            ApartmentOwnerId: apartment.OwnerId,
            Message: message);

        var systemNotification = Notification.Create(
            user.Id,
            NotificationType.ApartmentStaffAssignmentCreated,
            JsonSerializer.Serialize(payload),
            dateTimeProvider.UtcNow);

        notificationRepository.Add(systemNotification);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        await emailService.SendAsync(
            user.Email,
            "You have been assigned as maintenance staff",
            message);
    }
}