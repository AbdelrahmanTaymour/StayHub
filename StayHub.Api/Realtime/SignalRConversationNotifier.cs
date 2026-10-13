using Microsoft.AspNetCore.SignalR;
using StayHub.Api.Hubs;
using StayHub.Application.Abstractions.Realtime;

namespace StayHub.Api.Realtime;

internal sealed class SignalRConversationNotifier(IHubContext<ConversationHub> hubContext)
    : IRealtimeConversationNotifier
{
    public Task NotifyConversationUpdatedAsync(Guid conversationId, CancellationToken cancellationToken)
    {
        return hubContext.Clients
            .Group(ConversationHub.ConversationGroup(conversationId))
            .SendAsync("ConversationUpdated", conversationId, cancellationToken);
    }

    public Task NotifyInboxUpdatedAsync(IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken)
    {
        if (userIds.Count == 0) return Task.CompletedTask;

        var groups = userIds.Select(ConversationHub.UserGroup).ToArray();

        return hubContext.Clients.Groups(groups).SendAsync("InboxUpdated", cancellationToken);
    }
}