using MediatR;
using StayHub.Application.Abstractions.Email;
using StayHub.Domain.Users;
using StayHub.Domain.Users.Events;

namespace StayHub.Application.Users.RevokeUserSession;

public class UserSessionRevokedAlertDomainEventHandler(
    IUserRepository userRepository,
    IEmailService emailService) : INotificationHandler<UserSessionRevokedDomainEvent>
{
    public async Task Handle(UserSessionRevokedDomainEvent notification, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(notification.UserId, cancellationToken);

        if (user is null) return;

        await emailService.SendAsync(
            user.Email,
            "A session was signed out",
            "One of your active sessions was just revoked.");
    }
}