using System.Text.Json;
using MediatR;
using StayHub.Application.Abstractions.Clock;
using StayHub.Application.Abstractions.Email;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Apartments;
using StayHub.Domain.Apartments.Events;
using StayHub.Domain.Notifications;
using StayHub.Domain.Users;

namespace StayHub.Application.Apartments.ChangeApartmentStaffRole;

public sealed class ApartmentStaffAssignmentRoleChangedDomainEventHandler(
    IApartmentRepository apartmentRepository,
    IUserRepository userRepository,
    INotificationRepository notificationRepository,
    IUnitOfWork unitOfWork,
    IEmailService emailService,
    IDateTimeProvider dateTimeProvider)
    : INotificationHandler<ApartmentStaffAssignmentRoleChangedDomainEvent>
{
    public async Task Handle(
        ApartmentStaffAssignmentRoleChangedDomainEvent notification,
        CancellationToken cancellationToken)
    {
        var apartment = await apartmentRepository.GetByIdAsync(
            notification.ApartmentId,
            cancellationToken);

        if (apartment is null) return;

        var user = await userRepository.GetByIdAsync(notification.UserId, cancellationToken);

        if (user is null) return;

        var message =
            $"Your role for the apartment \"{apartment.Name}\" " +
            $"has changed from {notification.PreviousRole} " +
            $"to {notification.NewRole}.";

        var payload =
            new ApartmentStaffAssignmentRoleChangedNotificationPayload(
                AssignmentId: notification.AssignmentId,
                ApartmentId: apartment.Id,
                ApartmentOwnerId: apartment.OwnerId,
                PreviousRole: notification.PreviousRole,
                NewRole: notification.NewRole,
                Message: message);

        var systemNotification = Notification.Create(
            user.Id,
            NotificationType.ApartmentStaffAssignmentRoleChanged,
            JsonSerializer.Serialize(payload),
            dateTimeProvider.UtcNow);

        notificationRepository.Add(systemNotification);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        await emailService.SendAsync(
            user.Email,
            "Your apartment staff role has changed",
            message);
    }
}