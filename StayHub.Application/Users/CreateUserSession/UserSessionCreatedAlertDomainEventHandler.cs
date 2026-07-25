using MediatR;
using StayHub.Application.Abstractions.Email;
using StayHub.Domain.Users;
using StayHub.Domain.Users.Events;

namespace StayHub.Application.Users.CreateUserSession;

public class UserSessionCreatedAlertDomainEventHandler(
    IUserRepository userRepository,
    IUserSessionRepository userSessionRepository,
    IEmailService emailService) : INotificationHandler<UserSessionCreatedDomainEvent>
{
    public async Task Handle(UserSessionCreatedDomainEvent notification, CancellationToken cancellationToken)
    {
        var session = await userSessionRepository.GetByIdAsync(notification.Id, cancellationToken);

        if (session is null) return;

        var user = await userRepository.GetByIdAsync(notification.UserId, cancellationToken);

        if (user is null) return;

        await emailService.SendAsync(
            user.Email,
            "New sign-in to your account",
            $"A new sign-in was detected from {session.DeviceInfo} ({session.IpAddress}). If this wasn't you, revoke it from your account settings.");
    }
}