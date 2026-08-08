using MediatR;
using Microsoft.AspNetCore.Authorization;
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

namespace StayHub.Api.Controllers.Apartments;

[Authorize]
[ApiController]
[Route("api/v{version:apiVersion}/apartments")]
public sealed class ApartmentsController(ISender sender) : ControllerBase
{
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApartmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApartmentResponse>> GetApartment(Guid id, CancellationToken cancellationToken)
    {
        var query = new GetApartmentQuery(id);

        var result = await sender.Send(query, cancellationToken);

        return result.IsFailure ? result.ToProblemDetails(this) : Ok(result.Value);
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ApartmentSummaryResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ApartmentSummaryResponse>>> Search(
        [FromQuery] string? city,
        [FromQuery] decimal? minPrice,
        [FromQuery] decimal? maxPrice,
        [FromQuery] DateOnly? start,
        [FromQuery] DateOnly? end,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new SearchApartmentsQuery(city, minPrice, maxPrice, start, end, page, pageSize);

        var result = await sender.Send(query, cancellationToken);

        return result.IsFailure ? result.ToProblemDetails(this) : Ok(result.Value);
    }


    [HttpGet("by-owner/{ownerId:guid}")]
    [ProducesResponseType(typeof(IReadOnlyList<ApartmentSummaryResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ApartmentSummaryResponse>>> GetByOwner(
        Guid ownerId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new GetApartmentsByOwnerQuery(ownerId, page, pageSize);

        var result = await sender.Send(query, cancellationToken);

        return result.IsFailure ? result.ToProblemDetails(this) : Ok(result.Value);
    }

    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Guid>> Create(CreateApartmentRequest request, CancellationToken cancellationToken)
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
            ? result.ToProblemDetails(this)
            : CreatedAtAction(nameof(GetApartment), new { id = result.Value }, result.Value);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Update(Guid id, UpdateApartmentRequest request, CancellationToken cancellationToken)
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

        return result.IsFailure ? result.ToProblemDetails(this) : NoContent();
    }

    [HttpPost("{id:guid}/activate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult> Activate(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ActivateApartmentCommand(id), cancellationToken);

        return result.IsFailure ? result.ToProblemDetails(this) : NoContent();
    }

    [HttpPost("{id:guid}/deactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DeactivateApartmentCommand(id), cancellationToken);

        return result.IsFailure ? result.ToProblemDetails(this) : NoContent();
    }

    // ---- Amenities ----

    [HttpPost("{id:guid}/amenities")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult> AddAmenity(
        Guid id,
        AddApartmentAmenityRequest request,
        CancellationToken cancellationToken)
    {
        var command = new AddApartmentAmenityCommand(id, request.Amenity);

        var result = await sender.Send(command, cancellationToken);

        return result.IsFailure ? result.ToProblemDetails(this) : NoContent();
    }

    [HttpDelete("{id:guid}/amenities")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> RemoveAmenity(Guid id, Amenity amenity, CancellationToken cancellationToken)
    {
        var command = new RemoveApartmentAmenityCommand(id, amenity);

        var result = await sender.Send(command, cancellationToken);

        return result.IsFailure ? result.ToProblemDetails(this) : NoContent();
    }

    // ---- Images ----

    [HttpPost("{id:guid}/images")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Guid>> AddImage(
        Guid id,
        [FromForm] AddApartmentImageRequest request,
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
            ? result.ToProblemDetails(this)
            : CreatedAtAction(nameof(GetApartment), new { id }, result.Value);
    }

    [HttpDelete("images/{imageId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> RemoveImage(Guid imageId, CancellationToken cancellationToken)
    {
        var command = new RemoveApartmentImageCommand(imageId);

        var result = await sender.Send(command, cancellationToken);

        return result.IsFailure ? result.ToProblemDetails(this) : NoContent();
    }

    [HttpPut("{id:guid}/images/order")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> ReorderImages(
        Guid id,
        ReorderApartmentImagesRequest request,
        CancellationToken cancellationToken)
    {
        var command = new ReorderApartmentImagesCommand(id, request.OrderedImageIds);

        var result = await sender.Send(command, cancellationToken);

        return result.IsFailure ? result.ToProblemDetails(this) : NoContent();
    }

    // ---- Availability blocks ----

    [HttpPost("{id:guid}/availability-blocks")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<Guid>> CreateAvailabilityBlock(
        Guid id,
        CreateApartmentAvailabilityBlockRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateApartmentAvailabilityBlockCommand(
            id,
            request.Start,
            request.End,
            request.Reason);

        var result = await sender.Send(command, cancellationToken);

        return result.IsFailure ? result.ToProblemDetails(this) : Ok(result.Value);
    }

    [HttpDelete("availability-blocks/{blockId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> RemoveAvailabilityBlock(Guid blockId, CancellationToken cancellationToken)
    {
        var command = new RemoveApartmentAvailabilityBlockCommand(blockId);

        var result = await sender.Send(command, cancellationToken);

        return result.IsFailure ? result.ToProblemDetails(this) : NoContent();
    }

    // ---- Staff assignments ----

    [HttpPost("{id:guid}/staff")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<Guid>> AssignStaff(
        Guid id,
        AssignApartmentStaffRequest request,
        CancellationToken cancellationToken)
    {
        var command = new AssignApartmentStaffCommand(id, request.StaffUserId, request.Role);

        var result = await sender.Send(command, cancellationToken);

        return result.IsFailure ? result.ToProblemDetails(this) : Ok(result.Value);
    }

    [HttpDelete("staff/{assignmentId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> RevokeStaff(Guid assignmentId, CancellationToken cancellationToken)
    {
        var command = new RevokeApartmentStaffAssignmentCommand(assignmentId);

        var result = await sender.Send(command, cancellationToken);

        return result.IsFailure ? result.ToProblemDetails(this) : NoContent();
    }
}