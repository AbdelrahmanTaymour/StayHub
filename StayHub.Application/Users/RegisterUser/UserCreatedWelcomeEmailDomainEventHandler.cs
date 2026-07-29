using MediatR;
using StayHub.Application.Abstractions.Email;
using StayHub.Domain.Users;
using StayHub.Domain.Users.Events;

namespace StayHub.Application.Users.RegisterUser;

public class UserCreatedWelcomeEmailDomainEventHandler(
    IUserRepository userRepository,
    IEmailService emailService) : INotificationHandler<UserCreatedDomainEvent>
{
    public async Task Handle(UserCreatedDomainEvent notification, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(notification.UserId, cancellationToken);

        if (user is null) return;

        await emailService.SendAsync(user.Email, "Welcome to StayHub!", "Your account has been created.");
    }
}