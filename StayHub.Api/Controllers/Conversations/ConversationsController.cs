using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StayHub.Api.Extensions;
using StayHub.Application.Conversations.GetMessagesByConversation;
using StayHub.Application.Conversations.GetMyConversations;
using StayHub.Application.Conversations.MarkConversationAsRead;
using StayHub.Application.Conversations.SendMessage;
using StayHub.Application.Conversations.StartConversation;

namespace StayHub.Api.Controllers.Conversations;

[Authorize]
[ApiController]
[ApiVersion(ApiVersions.V1)]
[Route("api/v{version:apiVersion}/conversations")]
public sealed class ConversationsController(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ConversationSummaryResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ConversationSummaryResponse>>> GetMyConversations(
        CancellationToken cancellationToken)
    {
        var query = new GetMyConversationsQuery();

        var result = await sender.Send(query, cancellationToken);

        return result.IsFailure ? result.ToProblemDetails(this) : Ok(result.Value);
    }

    [HttpGet("{id:guid}/messages")]
    [ProducesResponseType(typeof(IReadOnlyList<MessageResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<MessageResponse>>> GetMessages(
        Guid id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new GetMessagesByConversationQuery(id, page, pageSize);

        var result = await sender.Send(query, cancellationToken);

        return result.IsFailure ? result.ToProblemDetails(this) : Ok(result.Value);
    }

    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Guid>> Start(StartConversationRequest request, CancellationToken cancellationToken)
    {
        var command = new StartConversationCommand(request.ApartmentId, request.InitialMessage);

        var result = await sender.Send(command, cancellationToken);

        return result.IsFailure
            ? result.ToProblemDetails(this)
            : CreatedAtAction(nameof(GetMessages), new { id = result.Value }, result.Value);
    }

    [HttpPost("{id:guid}/messages")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Guid>> SendMessage(
        Guid id,
        SendMessageRequest request,
        CancellationToken cancellationToken)
    {
        var command = new SendMessageCommand(id, request.Body);

        var result = await sender.Send(command, cancellationToken);

        return result.IsFailure
            ? result.ToProblemDetails(this)
            : CreatedAtAction(nameof(GetMessages), new { id }, result.Value);
    }

    [HttpPost("{id:guid}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> MarkAsRead(Guid id, CancellationToken cancellationToken)
    {
        var command = new MarkConversationAsReadCommand(id);

        var result = await sender.Send(command, cancellationToken);

        return result.IsFailure ? result.ToProblemDetails(this) : NoContent();
    }
}