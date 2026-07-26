using MediatR;
using StayHub.Application.Abstractions.Clock;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Conversations;
using StayHub.Domain.Conversations.Events;
using StayHub.Domain.Notifications;

namespace StayHub.Application.Conversations.SendMessage;

public class MessageSentDomainEventHandler(
    IMessageRepository messageRepository,
    IConversationRepository conversationRepository,
    INotificationRepository notificationRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : INotificationHandler<MessageSentDomainEvent>
{
    public async Task Handle(MessageSentDomainEvent notification, CancellationToken cancellationToken)
    {
        var message = await messageRepository.GetByIdAsync(notification.MessageId, cancellationToken);

        if (message is null) return;

        var conversation = await conversationRepository.GetByIdAsync(notification.ConversationId, cancellationToken);

        if (conversation is null) return;

        conversation.RegisterMessage(message.SentOnUtc);

        var recipientId = notification.SenderId == conversation.GuestId
            ? conversation.OwnerId
            : conversation.GuestId;

        var systemNotification = Notification.Create(
            recipientId,
            NotificationType.NewMessage,
            $"{{\"conversationId\":\"{conversation.Id}\",\"messageId\":\"{message.Id}\"}}",
            dateTimeProvider.UtcNow);

        notificationRepository.Add(systemNotification);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}