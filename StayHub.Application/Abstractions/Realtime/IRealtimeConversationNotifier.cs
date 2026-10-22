namespace StayHub.Application.Abstractions.Realtime;

public interface IRealtimeConversationNotifier
{
    /// <summary>A conversation has a new activity (message sent or read) — anyone with it open should refetch.</summary>
    Task NotifyConversationUpdatedAsync(Guid conversationId, CancellationToken cancellationToken);

    /// <summary>These users' conversation list (sidebar preview / unread counts) may be stale.</summary>
    Task NotifyInboxUpdatedAsync(IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken);
}