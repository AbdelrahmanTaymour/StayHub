using MediatR;
using StayHub.Api.Extensions;
using StayHub.Application.Maintenance.AssignMaintenanceRequestStaff;
using StayHub.Application.Maintenance.CloseMaintenanceRequest;
using StayHub.Application.Maintenance.CreateMaintenanceRequest;
using StayHub.Application.Maintenance.GetApartmentMaintenanceRequests;
using StayHub.Application.Maintenance.GetMaintenanceRequest;
using StayHub.Application.Maintenance.GetMaintenanceRequestForGuest;
using StayHub.Application.Maintenance.ResolveMaintenanceRequest;
using StayHub.Application.Maintenance.StartMaintenanceRequest;
using StayHub.Domain.Maintenance;

namespace StayHub.Api.Endpoints.Maintenance;

public static class MaintenanceEndpoints
{
    public static IEndpointRouteBuilder MapMaintenanceEndpoints(this IEndpointRouteBuilder builder)
    {
        var group = builder.MapGroup("apartments").WithTags("Maintenance").RequireAuthorization();

        group.MapGet("{id:guid}/maintenance-requests", GetMaintenanceRequestsByApartment)
            .HasPermission(Permissions.MaintenanceManage)
            .Produces<IReadOnlyList<MaintenanceRequestsResponse>>()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("maintenance-requests/{requestId:guid}", GetMaintenanceRequest)
            .HasPermission(Permissions.MaintenanceManage)
            .WithName(nameof(GetMaintenanceRequest))
            .Produces<MaintenanceRequestResponse>()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("maintenance-requests/{requestId:guid}/guest", GetMaintenanceRequestForGuest)
            .WithName(nameof(GetMaintenanceRequestForGuest))
            .Produces<GuestMaintenanceRequestResponse>()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

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

        group.MapPost("maintenance-requests/{requestId:guid}/assign", AssignMaintenanceRequestStaff)
            .HasPermission(Permissions.MaintenanceManage)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return builder;
    }

    private static async Task<IResult> GetMaintenanceRequestsByApartment(
        Guid id,
        ISender sender,
        CancellationToken cancellationToken,
        string? search = null,
        MaintenanceRequestStatus? status = null,
        int page = 1,
        int pageSize = 20)
    {
        var query = new GetApartmentMaintenanceRequestsQuery(id, search, status, page, pageSize);

        var result = await sender.Send(query, cancellationToken);

        return result.IsFailure ? result.ToProblemDetails() : Results.Ok(result.Value);
    }

    private static async Task<IResult> GetMaintenanceRequest(
        Guid requestId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetMaintenanceRequestQuery(requestId), cancellationToken);

        return result.IsFailure ? result.ToProblemDetails() : Results.Ok(result.Value);
    }

    private static async Task<IResult> GetMaintenanceRequestForGuest(
        Guid requestId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetMaintenanceRequestForGuestQuery(requestId), cancellationToken);

        return result.IsFailure ? result.ToProblemDetails() : Results.Ok(result.Value);
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

    private static async Task<IResult> AssignMaintenanceRequestStaff(
        Guid requestId,
        AssignMaintenanceRequestStaffRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new AssignMaintenanceRequestStaffCommand(requestId, request.StaffUserId);

        var result = await sender.Send(command, cancellationToken);

        return result.IsFailure ? result.ToProblemDetails() : Results.NoContent();
    }

    private sealed record AssignMaintenanceRequestStaffRequest(Guid StaffUserId);
}