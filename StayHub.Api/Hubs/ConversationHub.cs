using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using StayHub.Domain.Conversations;
using StayHub.Infrastructure.Authentication;

namespace StayHub.Api.Hubs;

[Authorize]
public sealed class ConversationHub(IConversationRepository conversationRepository) : Hub
{
    public static string ConversationGroup(Guid conversationId) => $"conversation:{conversationId}";
    public static string UserGroup(Guid userId) => $"user:{userId}";

    public override async Task OnConnectedAsync()
    {
        var userId = Context.User.GetUserId();

        await Groups.AddToGroupAsync(Context.ConnectionId, UserGroup(userId));

        await base.OnConnectedAsync();
    }

    public async Task JoinConversation(Guid conversationId)
    {
        var userId = Context.User.GetUserId();

        var conversation = await conversationRepository.GetByIdAsync(conversationId, Context.ConnectionAborted);

        if (conversation is null || (conversation.GuestId != userId && conversation.OwnerId != userId))
        {
            throw new HubException("Not authorized to join this conversation.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, ConversationGroup(conversationId));
    }

    public async Task LeaveConversation(Guid conversationId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, ConversationGroup(conversationId));
    }
}