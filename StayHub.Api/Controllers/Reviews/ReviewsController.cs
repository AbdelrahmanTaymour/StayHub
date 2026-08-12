using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StayHub.Api.Extensions;
using StayHub.Application.Reviews.CreateReview;
using StayHub.Application.Reviews.CreateReviewResponse;
using StayHub.Application.Reviews.GetReview;
using StayHub.Application.Reviews.GetReviewsByApartment;
using StayHub.Infrastructure.Authorization;

namespace StayHub.Api.Controllers.Reviews;

[Authorize]
[ApiController]
[ApiVersion(ApiVersions.V1)]
[Route("api/v{version:apiVersion}/reviews")]
public sealed class ReviewsController(ISender sender) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ReviewResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReviewResponse>> GetReview(Guid id, CancellationToken cancellationToken)
    {
        var query = new GetReviewQuery(id);

        var result = await sender.Send(query, cancellationToken);

        return result.IsFailure ? result.ToProblemDetails(this) : Ok(result.Value);
    }

    [AllowAnonymous]
    [HttpGet("by-apartment/{apartmentId:guid}")]
    [ProducesResponseType(typeof(IReadOnlyList<ReviewListItemResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ReviewListItemResponse>>> GetByApartment(
        Guid apartmentId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new GetReviewsByApartmentQuery(apartmentId, page, pageSize);

        var result = await sender.Send(query, cancellationToken);

        return result.IsFailure ? result.ToProblemDetails(this) : Ok(result.Value);
    }

    [HttpPost]
    [HasPermission(Permissions.ReviewCreate)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<Guid>> Create(CreateReviewRequest request, CancellationToken cancellationToken)
    {
        var command = new CreateReviewCommand(request.BookingId, request.Rating, request.Comment);

        var result = await sender.Send(command, cancellationToken);

        return result.IsFailure
            ? result.ToProblemDetails(this)
            : CreatedAtAction(nameof(GetReview), new { id = result.Value }, result.Value);
    }

    [HttpPost("{reviewId:guid}/response")]
    [HasPermission(Permissions.ReviewRespond)]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<Guid>> CreateResponse(
        Guid reviewId,
        CreateReviewResponseRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateReviewResponseCommand(reviewId, request.Comment);

        var result = await sender.Send(command, cancellationToken);

        return result.IsFailure
            ? result.ToProblemDetails(this)
            : CreatedAtAction(nameof(GetReview), new { id = reviewId }, result.Value);
    }
}