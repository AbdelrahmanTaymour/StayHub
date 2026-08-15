using MediatR;
using StayHub.Api.Extensions;
using StayHub.Application.Conversations.GetMessagesByConversation;
using StayHub.Application.Conversations.GetMyConversations;
using StayHub.Application.Conversations.MarkConversationAsRead;
using StayHub.Application.Conversations.SendMessage;
using StayHub.Application.Conversations.StartConversation;

namespace StayHub.Api.Endpoints.Conversations;

public static class ConversationEndpoints
{
    public static IEndpointRouteBuilder MapConversationEndpoints(this IEndpointRouteBuilder builder)
    {
        var group = builder.MapGroup("conversations").WithTags("Conversations").RequireAuthorization();

        group.MapGet("", GetMyConversations)
            .Produces<IReadOnlyList<ConversationSummaryResponse>>();

        group.MapGet("{id:guid}/messages", GetMessages)
            .WithName(nameof(GetMessages))
            .Produces<IReadOnlyList<MessageResponse>>()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("", Start)
            .Produces<Guid>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("{id:guid}/messages", SendMessage)
            .Produces<Guid>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("{id:guid}/read", MarkAsRead)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return builder;
    }

    private static async Task<IResult> GetMyConversations(ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetMyConversationsQuery(), cancellationToken);

        return result.IsFailure ? result.ToProblemDetails() : Results.Ok(result.Value);
    }

    private static async Task<IResult> GetMessages(
        Guid id,
        ISender sender,
        CancellationToken cancellationToken,
        int page = 1,
        int pageSize = 20)
    {
        var result = await sender.Send(new GetMessagesByConversationQuery(id, page, pageSize), cancellationToken);

        return result.IsFailure ? result.ToProblemDetails() : Results.Ok(result.Value);
    }

    private static async Task<IResult> Start(
        StartConversationRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new StartConversationCommand(request.ApartmentId, request.InitialMessage);

        var result = await sender.Send(command, cancellationToken);

        return result.IsFailure
            ? result.ToProblemDetails()
            : TypedResults.CreatedAtRoute(result.Value, nameof(GetMessages), new { id = result.Value });
    }

    private static async Task<IResult> SendMessage(
        Guid id,
        SendMessageRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new SendMessageCommand(id, request.Body), cancellationToken);

        return result.IsFailure
            ? result.ToProblemDetails()
            : TypedResults.CreatedAtRoute(result.Value, nameof(GetMessages), new { id });
    }

    private static async Task<IResult> MarkAsRead(Guid id, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new MarkConversationAsReadCommand(id), cancellationToken);

        return result.IsFailure ? result.ToProblemDetails() : Results.NoContent();
    }
}