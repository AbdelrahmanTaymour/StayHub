using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StayHub.Api.Extensions;
using StayHub.Application.Maintenance.CloseMaintenanceRequest;
using StayHub.Application.Maintenance.CreateMaintenanceRequest;
using StayHub.Application.Maintenance.ResolveMaintenanceRequest;
using StayHub.Application.Maintenance.StartMaintenanceRequest;
using StayHub.Infrastructure.Authorization;

namespace StayHub.Api.Controllers.Maintenance;

[Authorize]
[ApiController]
[ApiVersion(ApiVersions.V1)]
[Route("api/v{version:apiVersion}/apartments")]
public sealed class MaintenanceController(ISender sender) : ControllerBase
{
    [HttpPost("{id:guid}/maintenance-requests")]
    [HasPermission(Permissions.MaintenanceCreate)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Guid>> CreateMaintenanceRequest(
        Guid id,
        CreateMaintenanceRequestRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateMaintenanceRequestCommand(id, request.Title, request.Description);

        var result = await sender.Send(command, cancellationToken);

        return result.IsFailure
            ? result.ToProblemDetails(this)
            : CreatedAtAction(nameof(CreateMaintenanceRequest), new { id = result.Value }, result.Value);
    }

    [HttpPost("maintenance-requests/{requestId:guid}/start")]
    [HasPermission(Permissions.MaintenanceManage)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult> StartMaintenanceRequest(Guid requestId, CancellationToken cancellationToken)
    {
        var command = new StartMaintenanceRequestCommand(requestId);

        var result = await sender.Send(command, cancellationToken);

        return result.IsFailure ? result.ToProblemDetails(this) : NoContent();
    }

    [HttpPost("maintenance-requests/{requestId:guid}/resolve")]
    [HasPermission(Permissions.MaintenanceManage)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult> ResolveMaintenanceRequest(Guid requestId, CancellationToken cancellationToken)
    {
        var command = new ResolveMaintenanceRequestCommand(requestId);

        var result = await sender.Send(command, cancellationToken);

        return result.IsFailure ? result.ToProblemDetails(this) : NoContent();
    }

    [HttpPost("maintenance-requests/{requestId:guid}/close")]
    [HasPermission(Permissions.MaintenanceManage)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult> CloseMaintenanceRequest(Guid requestId, CancellationToken cancellationToken)
    {
        var command = new CloseMaintenanceRequestCommand(requestId);

        var result = await sender.Send(command, cancellationToken);

        return result.IsFailure ? result.ToProblemDetails(this) : NoContent();
    }
}