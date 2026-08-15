using MediatR;
using Microsoft.AspNetCore.Mvc;
using StayHub.Api.Extensions;
using StayHub.Application.Apartments.ActivateApartment;
using StayHub.Application.Apartments.AddApartmentAmenity;
using StayHub.Application.Apartments.AddApartmentImage;
using StayHub.Application.Apartments.AssignApartmentStaff;
using StayHub.Application.Apartments.CreateApartment;
using StayHub.Application.Apartments.CreateApartmentAvailabilityBlock;
using StayHub.Application.Apartments.DeactivateApartment;
using StayHub.Application.Apartments.GetApartment;
using StayHub.Application.Apartments.GetApartmentsByOwner;
using StayHub.Application.Apartments.RemoveApartmentAmenity;
using StayHub.Application.Apartments.RemoveApartmentAvailabilityBlock;
using StayHub.Application.Apartments.RemoveApartmentImage;
using StayHub.Application.Apartments.ReorderApartmentImages;
using StayHub.Application.Apartments.RevokeApartmentStaffAssignment;
using StayHub.Application.Apartments.SearchApartments;
using StayHub.Application.Apartments.UpdateApartment;
using StayHub.Domain.Apartments;

namespace StayHub.Api.Endpoints.Apartments;

public static class ApartmentEndpoints
{
    public static IEndpointRouteBuilder MapApartmentEndpoints(this IEndpointRouteBuilder builder)
    {
        var group = builder.MapGroup("apartments")
            .WithTags("Apartments")
            .RequireAuthorization();

        group.MapGet("{id:guid}", GetApartment)
            .AllowAnonymous()
            .WithName(nameof(GetApartment))
            .Produces<ApartmentResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("", Search)
            .AllowAnonymous()
            .Produces<IReadOnlyList<ApartmentSummaryResponse>>();

        group.MapGet("by-owner/{ownerId:guid}", GetByOwner)
            .AllowAnonymous()
            .Produces<IReadOnlyList<ApartmentSummaryResponse>>();

        group.MapPost("", Create)
            .HasPermission(Permissions.ApartmentCreate)
            .Produces<Guid>(StatusCodes.Status201Created)
            .ProducesValidationProblem();

        group.MapPut("{id:guid}", Update)
            .HasPermission(Permissions.ApartmentManage)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("{id:guid}/activate", Activate)
            .HasPermission(Permissions.ApartmentManage)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("{id:guid}/deactivate", Deactivate)
            .HasPermission(Permissions.ApartmentManage)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        // ---- Amenities ----

        group.MapPost("{id:guid}/amenities", AddAmenity)
            .HasPermission(Permissions.ApartmentManage)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapDelete("{id:guid}/amenities", RemoveAmenity)
            .HasPermission(Permissions.ApartmentManage)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        // ---- Images ----

        group.MapPost("{id:guid}/images", AddImage)
            .HasPermission(Permissions.ApartmentManage)
            .DisableAntiforgery()
            .Produces<Guid>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapDelete("images/{imageId:guid}", RemoveImage)
            .HasPermission(Permissions.ApartmentManage)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPut("{id:guid}/images/order", ReorderImages)
            .HasPermission(Permissions.ApartmentManage)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        // ---- Availability blocks ----

        group.MapPost("{id:guid}/availability-blocks", CreateAvailabilityBlock)
            .HasPermission(Permissions.ApartmentManage)
            .WithName(nameof(CreateAvailabilityBlock))
            .Produces<Guid>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapDelete("availability-blocks/{blockId:guid}", RemoveAvailabilityBlock)
            .HasPermission(Permissions.ApartmentManage)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        // ---- Staff assignments ----

        group.MapPost("{id:guid}/staff", AssignStaff)
            .HasPermission(Permissions.ApartmentManage)
            .Produces<Guid>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapDelete("staff/{assignmentId:guid}", RevokeStaff)
            .HasPermission(Permissions.ApartmentManage)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return group;
    }

    public static async Task<IResult> GetApartment(Guid id, ISender sender, CancellationToken cancellationToken)
    {
        var query = new GetApartmentQuery(id);

        var result = await sender.Send(query, cancellationToken);

        return result.IsFailure ? result.ToProblemDetails() : Results.Ok(result.Value);
    }

    private static async Task<IResult> Search(
        ISender sender,
        CancellationToken cancellationToken,
        string? city = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        DateOnly? start = null,
        DateOnly? end = null,
        int page = 1,
        int pageSize = 20)
    {
        var query = new SearchApartmentsQuery(city, minPrice, maxPrice, start, end, page, pageSize);

        var result = await sender.Send(query, cancellationToken);

        return result.IsFailure ? result.ToProblemDetails() : Results.Ok(result.Value);
    }


    private static async Task<IResult> GetByOwner(
        Guid ownerId,
        ISender sender,
        CancellationToken cancellationToken,
        int page = 1,
        int pageSize = 20)
    {
        var result = await sender.Send(new GetApartmentsByOwnerQuery(ownerId, page, pageSize), cancellationToken);

        return result.IsFailure ? result.ToProblemDetails() : Results.Ok(result.Value);
    }

    private static async Task<IResult> Create(
        CreateApartmentRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new CreateApartmentCommand(
            request.Name,
            request.Description,
            request.Street,
            request.City,
            request.State,
            request.ZipCode,
            request.Country,
            request.PriceAmount,
            request.PriceCurrency,
            request.CleaningFeeAmount,
            request.CleaningFeeCurrency);

        var result = await sender.Send(command, cancellationToken);

        return result.IsFailure
            ? result.ToProblemDetails()
            : TypedResults.CreatedAtRoute(result.Value, nameof(GetApartment), new { id = result.Value });
    }

    private static async Task<IResult> Update(
        Guid id,
        UpdateApartmentRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new UpdateApartmentCommand(
            id,
            request.Name,
            request.Description,
            request.PriceAmount,
            request.PriceCurrency,
            request.CleaningFeeAmount,
            request.CleaningFeeCurrency);

        var result = await sender.Send(command, cancellationToken);

        return result.IsFailure ? result.ToProblemDetails() : Results.NoContent();
    }

    private static async Task<IResult> Activate(Guid id, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ActivateApartmentCommand(id), cancellationToken);

        return result.IsFailure ? result.ToProblemDetails() : Results.NoContent();
    }

    private static async Task<IResult> Deactivate(Guid id, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DeactivateApartmentCommand(id), cancellationToken);

        return result.IsFailure ? result.ToProblemDetails() : Results.NoContent();
    }

    private static async Task<IResult> AddAmenity(
        Guid id,
        AddApartmentAmenityRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new AddApartmentAmenityCommand(id, request.Amenity), cancellationToken);

        return result.IsFailure ? result.ToProblemDetails() : Results.NoContent();
    }

    private static async Task<IResult> RemoveAmenity(
        Guid id,
        Amenity amenity,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RemoveApartmentAmenityCommand(id, amenity), cancellationToken);

        return result.IsFailure ? result.ToProblemDetails() : Results.NoContent();
    }

    private static async Task<IResult> AddImage(
        Guid id,
        [FromForm] AddApartmentImageRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        await using var stream = request.File.OpenReadStream();

        var command = new AddApartmentImageCommand(
            id,
            stream,
            request.File.FileName,
            request.File.ContentType,
            request.IsPrimary);

        var result = await sender.Send(command, cancellationToken);

        return result.IsFailure
            ? result.ToProblemDetails()
            : TypedResults.CreatedAtRoute(result.Value, nameof(GetApartment), new { id });
    }

    private static async Task<IResult> RemoveImage(Guid imageId, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RemoveApartmentImageCommand(imageId), cancellationToken);

        return result.IsFailure ? result.ToProblemDetails() : Results.NoContent();
    }

    private static async Task<IResult> ReorderImages(
        Guid id,
        ReorderApartmentImagesRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new ReorderApartmentImagesCommand(id, request.OrderedImageIds),
            cancellationToken);

        return result.IsFailure ? result.ToProblemDetails() : Results.NoContent();
    }

    private static async Task<IResult> CreateAvailabilityBlock(
        Guid id,
        CreateApartmentAvailabilityBlockRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new CreateApartmentAvailabilityBlockCommand(id, request.Start, request.End, request.Reason);

        var result = await sender.Send(command, cancellationToken);

        return result.IsFailure ? result.ToProblemDetails() : Results.Ok(result.Value);
    }

    private static async Task<IResult> RemoveAvailabilityBlock(
        Guid blockId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RemoveApartmentAvailabilityBlockCommand(blockId), cancellationToken);

        return result.IsFailure ? result.ToProblemDetails() : Results.NoContent();
    }

    private static async Task<IResult> AssignStaff(
        Guid id,
        AssignApartmentStaffRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new AssignApartmentStaffCommand(id, request.StaffUserId, request.Role);

        var result = await sender.Send(command, cancellationToken);

        return result.IsFailure ? result.ToProblemDetails() : Results.Ok(result.Value);
    }

    private static async Task<IResult> RevokeStaff(
        Guid assignmentId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RevokeApartmentStaffAssignmentCommand(assignmentId), cancellationToken);

        return result.IsFailure ? result.ToProblemDetails() : Results.NoContent();
    }
}