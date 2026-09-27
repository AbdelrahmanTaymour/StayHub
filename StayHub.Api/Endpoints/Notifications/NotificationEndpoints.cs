using MediatR;
using StayHub.Api.Extensions;
using StayHub.Application.Notifications.GetMyNotifications;
using StayHub.Application.Notifications.MarkNotificationAsRead;
using StayHub.Domain.Abstractions;

namespace StayHub.Api.Endpoints.Notifications;

public static class NotificationEndpoints
{
    public static IEndpointRouteBuilder MapNotificationEndpoints(this IEndpointRouteBuilder builder)
    {
        var group = builder.MapGroup("notifications")
            .WithTags("Notifications")
            .RequireAuthorization();

        group.MapGet("", Get)
            .Produces<PagedResponse<MyNotificationsResponse>>();

        group.MapPost("{notificationId:guid}/read", MarkAsRead)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return builder;
    }

    private static async Task<IResult> Get(
        ISender sender,
        CancellationToken cancellationToken,
        bool unreadOnly = false,
        int page = 1,
        int pageSize = 20)
    {
        var result = await sender.Send(new GetMyNotificationsQuery(unreadOnly, page, pageSize), cancellationToken);

        return result.IsFailure ? result.ToProblemDetails() : Results.Ok(result.Value);
    }

    private static async Task<IResult> MarkAsRead(
        Guid notificationId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new MarkNotificationAsReadCommand(notificationId), cancellationToken);

        return result.IsFailure ? result.ToProblemDetails() : Results.NoContent();
    }
}