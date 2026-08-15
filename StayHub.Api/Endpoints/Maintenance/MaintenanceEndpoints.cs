using MediatR;
using StayHub.Api.Extensions;
using StayHub.Application.Maintenance.CloseMaintenanceRequest;
using StayHub.Application.Maintenance.CreateMaintenanceRequest;
using StayHub.Application.Maintenance.ResolveMaintenanceRequest;
using StayHub.Application.Maintenance.StartMaintenanceRequest;

namespace StayHub.Api.Endpoints.Maintenance;

public static class MaintenanceEndpoints
{
    public static IEndpointRouteBuilder MapMaintenanceEndpoints(this IEndpointRouteBuilder builder)
    {
        var group = builder.MapGroup("apartments").WithTags("Maintenance").RequireAuthorization();

        group.MapPost("{id:guid}/maintenance-requests", CreateMaintenanceRequest)
            .HasPermission(Permissions.MaintenanceCreate)
            .WithName(nameof(CreateMaintenanceRequest))
            .Produces<Guid>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("maintenance-requests/{requestId:guid}/start", StartMaintenanceRequest)
            .HasPermission(Permissions.MaintenanceManage)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("maintenance-requests/{requestId:guid}/resolve", ResolveMaintenanceRequest)
            .HasPermission(Permissions.MaintenanceManage)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("maintenance-requests/{requestId:guid}/close", CloseMaintenanceRequest)
            .HasPermission(Permissions.MaintenanceManage)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return builder;
    }

    private static async Task<IResult> CreateMaintenanceRequest(
        Guid id,
        CreateMaintenanceRequestRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new CreateMaintenanceRequestCommand(id, request.Title, request.Description);

        var result = await sender.Send(command, cancellationToken);

        return result.IsFailure
            ? result.ToProblemDetails()
            : TypedResults.CreatedAtRoute(
                result.Value,
                nameof(CreateMaintenanceRequest),
                new { id = result.Value });
    }

    private static async Task<IResult> StartMaintenanceRequest(
        Guid requestId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new StartMaintenanceRequestCommand(requestId), cancellationToken);

        return result.IsFailure ? result.ToProblemDetails() : Results.NoContent();
    }

    private static async Task<IResult> ResolveMaintenanceRequest(
        Guid requestId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ResolveMaintenanceRequestCommand(requestId), cancellationToken);

        return result.IsFailure ? result.ToProblemDetails() : Results.NoContent();
    }

    private static async Task<IResult> CloseMaintenanceRequest(
        Guid requestId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CloseMaintenanceRequestCommand(requestId), cancellationToken);

        return result.IsFailure ? result.ToProblemDetails() : Results.NoContent();
    }
}