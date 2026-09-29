using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using StayHub.Api.Extensions;
using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Apartments.ActivateApartment;
using StayHub.Application.Apartments.AddApartmentAmenity;
using StayHub.Application.Apartments.AddApartmentImage;
using StayHub.Application.Apartments.AssignApartmentStaff;
using StayHub.Application.Apartments.CreateApartment;
using StayHub.Application.Apartments.CreateApartmentAvailabilityBlock;
using StayHub.Application.Apartments.DeactivateApartment;
using StayHub.Application.Apartments.GetApartment;
using StayHub.Application.Apartments.GetApartmentAmenities;
using StayHub.Application.Apartments.GetApartmentAvailabilityBlocks;
using StayHub.Application.Apartments.GetApartmentForEdit;
using StayHub.Application.Apartments.GetApartmentImages;
using StayHub.Application.Apartments.GetApartmentPricing;
using StayHub.Application.Apartments.GetApartmentsByOwner;
using StayHub.Application.Apartments.GetApartmentStaff;
using StayHub.Application.Apartments.GetMyApartments;
using StayHub.Application.Apartments.GetMyApartmentsDashboard;
using StayHub.Application.Apartments.RemoveApartmentAmenity;
using StayHub.Application.Apartments.RemoveApartmentAvailabilityBlock;
using StayHub.Application.Apartments.RemoveApartmentImage;
using StayHub.Application.Apartments.ReorderApartmentImages;
using StayHub.Application.Apartments.RevokeApartmentStaffAssignment;
using StayHub.Application.Apartments.SearchApartments;
using StayHub.Application.Apartments.SearchStaffCandidate;
using StayHub.Application.Apartments.SetAsPrimaryImage;
using StayHub.Application.Apartments.UpdateApartment;
using StayHub.Application.Users.InviteUser;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Apartments;
using ApartmentPricingResponse = StayHub.Application.Apartments.GetApartmentPricing.ApartmentPricingResponse;

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

        group.MapGet("mine", GetMine)
            .HasPermission(Permissions.ApartmentManage)
            .WithName(nameof(GetMine))
            .Produces<PagedResponse<MyApartmentsResponse>>();

        group.MapGet("mine/dashboard", GetMineDashboard)
            .HasPermission(Permissions.ApartmentManage)
            .WithName(nameof(GetMineDashboard))
            .Produces<MyApartmentsDashboardResponse>();

        group.MapGet("{apartmentId:guid}/edit", GetForEdit)
            .HasPermission(Permissions.ApartmentManage)
            .WithName(nameof(GetForEdit))
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .Produces<ApartmentForEditResponse>();

        group.MapGet("", Search)
            .AllowAnonymous()
            .Produces<PagedResponse<SearchApartmentsResponse>>();

        group.MapGet("by-owner/{ownerId:guid}", GetByOwner)
            .AllowAnonymous()
            .Produces<IReadOnlyList<OwnerApartmentsResponse>>();

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

        group.Map("{apartmentId:guid}/pricing", GetPricing)
            .AllowAnonymous()
            .Produces<ApartmentPricingResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        // ---- Amenities ----

        group.MapGet("{apartmentId:guid}/amenities", GetAmenities)
            .HasPermission(Permissions.ApartmentManage)
            .WithName(nameof(GetAmenities))
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces<ApartmentAmenitiesResponse>();

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

        group.MapGet("{apartmentId:guid}/images", GetImages)
            .HasPermission(Permissions.ApartmentManage)
            .WithName(nameof(GetImages))
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces<ApartmentImagesResponse>();

        group.MapPost("{id:guid}/images", AddImage)
            .HasPermission(Permissions.ApartmentManage)
            .DisableAntiforgery()
            .Produces<Guid>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
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

        group.MapPut("{id:guid}/images/{imageId:guid}/primary", SetAsPrimaryImage)
            .HasPermission(Permissions.ApartmentManage)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        // ---- Availability blocks ----

        group.MapGet("{apartmentId:guid}/availability-blocks", GetAvailabilityBlocks)
            .AllowAnonymous()
            .WithName(nameof(GetAvailabilityBlocks))
            .Produces<ApartmentAvailabilityResponse>()
            .Produces(StatusCodes.Status404NotFound);


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

        group.MapGet("{apartmentId:guid}/staff", GetStaff)
            .HasPermission(Permissions.ApartmentManage)
            .Produces<IReadOnlyList<ApartmentStaffResponse>>()
            .Produces(StatusCodes.Status404NotFound);

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

        group.MapGet("{apartmentId:guid}/staff/search", SearchStaffCandidate)
            .HasPermission(Permissions.ApartmentManage)
            .Produces<StaffCandidateResponse>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("{id:guid}/staff/invite", InviteStaff)
            .HasPermission(Permissions.ApartmentManage)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return group;
    }

    private static async Task<IResult> GetApartment(Guid id, ISender sender, CancellationToken cancellationToken)
    {
        var query = new GetApartmentQuery(id);

        var result = await sender.Send(query, cancellationToken);

        return result.IsFailure ? result.ToProblemDetails() : Results.Ok(result.Value);
    }

    private static async Task<IResult> GetMine(
        ISender sender,
        IUserContext userContext,
        CancellationToken cancellationToken,
        MyApartmentsFilter status = MyApartmentsFilter.All,
        string? search = null,
        int page = 1,
        int pageSize = 10)
    {
        var result = await sender.Send(new GetMyApartmentsQuery(userContext.UserId, status, search, page, pageSize),
            cancellationToken);
        return result.IsFailure ? result.ToProblemDetails() : TypedResults.Ok(result.Value);
    }

    private static async Task<IResult> GetMineDashboard(
        ISender sender,
        IUserContext userContext,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetMyApartmentsDashboardQuery(userContext.UserId), cancellationToken);
        return result.IsFailure ? result.ToProblemDetails() : TypedResults.Ok(result.Value);
    }

    private static async Task<IResult> GetForEdit(
        Guid apartmentId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetApartmentForEditQuery(apartmentId), cancellationToken);

        return result.IsFailure ? result.ToProblemDetails() : TypedResults.Ok(result.Value);
    }


    private static async Task<IResult> Search(
        [AsParameters] SearchApartmentsQuery query,
        ISender sender,
        [FromServices] IValidator<SearchApartmentsQuery> validator,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(query, cancellationToken);

        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        var result = await sender.Send(query, cancellationToken);

        return result.IsFailure ? result.ToProblemDetails() : Results.Ok(result.Value);
    }

    private static async Task<IResult> GetByOwner(
        Guid ownerId,
        ISender sender,
        CancellationToken cancellationToken,
        OwnerApartmentsSort sort = OwnerApartmentsSort.PriceAsc,
        int page = 1,
        int pageSize = 9)
    {
        var result = await sender.Send(
            new GetApartmentsByOwnerQuery(
                OwnerId: ownerId,
                Sort: sort,
                Page: page,
                PageSize: pageSize),
            cancellationToken);

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

    private static async Task<IResult> GetPricing(
        [FromRoute] Guid apartmentId,
        [FromQuery] DateOnly start,
        [FromQuery] DateOnly end,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GetApartmentPricingQuery(apartmentId, start, end),
            cancellationToken);

        return result.IsFailure ? result.ToProblemDetails() : Results.Ok(result.Value);
    }

    private static async Task<IResult> GetAmenities(
        Guid apartmentId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetApartmentAmenitiesQuery(apartmentId), cancellationToken);

        return result.IsFailure ? result.ToProblemDetails() : TypedResults.Ok(result.Value);
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

    private static async Task<IResult> GetImages(
        Guid apartmentId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetApartmentImagesQuery(apartmentId), cancellationToken);

        return result.IsFailure ? result.ToProblemDetails() : TypedResults.Ok(result.Value);
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

    private static async Task<IResult> SetAsPrimaryImage(
        Guid id,
        Guid imageId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new SetAsPrimaryImageCommand(id, imageId),
            cancellationToken);

        return result.IsFailure
            ? result.ToProblemDetails()
            : Results.NoContent();
    }

    private static async Task<IResult> GetAvailabilityBlocks(
        Guid apartmentId,
        ISender sender,
        CancellationToken cancellationToken,
        int? year = null,
        int? month = null)
    {
        var result = await sender.Send(
            new GetApartmentAvailabilityBlocksQuery(apartmentId, year, month), cancellationToken);

        return result.IsFailure ? result.ToProblemDetails() : TypedResults.Ok(result.Value);
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

    private static async Task<IResult> GetStaff(
        Guid apartmentId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetApartmentStaffQuery(apartmentId), cancellationToken);

        return result.IsFailure ? result.ToProblemDetails() : TypedResults.Ok(result.Value);
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

    private static async Task<IResult> SearchStaffCandidate(
        Guid apartmentId,
        string email,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new SearchStaffCandidateQuery(apartmentId, email), cancellationToken);

        return result.IsFailure ? result.ToProblemDetails() : TypedResults.Ok(result.Value);
    }

    private static async Task<IResult> InviteStaff(
        Guid id,
        InviteStaffRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new InviteUserCommand(id, request.Email, request.Body), cancellationToken);

        return result.IsFailure ? result.ToProblemDetails() : Results.NoContent();
    }
}