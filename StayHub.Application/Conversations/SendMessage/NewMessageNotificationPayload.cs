namespace StayHub.Application.Conversations.SendMessage;

public sealed record NewMessageNotificationPayload(
    Guid ConversationId,
    Guid MessageId,
    string Message);