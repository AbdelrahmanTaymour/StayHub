using MediatR;
using StayHub.Api.Extensions;
using StayHub.Application.Reviews.CreateReview;
using StayHub.Application.Reviews.CreateReviewResponse;
using StayHub.Application.Reviews.GetReview;
using StayHub.Application.Reviews.GetReviewsByApartment;

namespace StayHub.Api.Endpoints.Reviews;

public static class ReviewEndpoints
{
    public static IEndpointRouteBuilder MapReviewEndpoints(this IEndpointRouteBuilder builder)
    {
        var group = builder.MapGroup("reviews").WithTags("Reviews").RequireAuthorization();

        group.MapGet("{id:guid}", GetReview)
            .AllowAnonymous()
            .WithName(nameof(GetReview))
            .Produces<ReviewResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("by-apartment/{apartmentId:guid}", GetByApartment)
            .AllowAnonymous()
            .Produces<IReadOnlyList<ReviewListItemResponse>>();

        group.MapPost("", Create)
            .HasPermission(Permissions.ReviewCreate)
            .Produces<Guid>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("{reviewId:guid}/response", CreateResponse)
            .HasPermission(Permissions.ReviewRespond)
            .Produces<Guid>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return builder;
    }

    private static async Task<IResult> GetReview(Guid id, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetReviewQuery(id), cancellationToken);

        return result.IsFailure ? result.ToProblemDetails() : Results.Ok(result.Value);
    }

    private static async Task<IResult> GetByApartment(
        Guid apartmentId,
        ISender sender,
        CancellationToken cancellationToken,
        int page = 1,
        int pageSize = 20)
    {
        var result = await sender.Send(new GetReviewsByApartmentQuery(apartmentId, page, pageSize), cancellationToken);

        return result.IsFailure ? result.ToProblemDetails() : Results.Ok(result.Value);
    }

    private static async Task<IResult> Create(
        CreateReviewRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new CreateReviewCommand(request.BookingId, request.Rating, request.Comment);

        var result = await sender.Send(command, cancellationToken);

        return result.IsFailure
            ? result.ToProblemDetails()
            : TypedResults.CreatedAtRoute(result.Value, nameof(GetReview), new { id = result.Value });
    }

    private static async Task<IResult> CreateResponse(
        Guid reviewId,
        CreateReviewResponseRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreateReviewResponseCommand(reviewId, request.Comment), cancellationToken);

        return result.IsFailure
            ? result.ToProblemDetails()
            : TypedResults.CreatedAtRoute(result.Value, nameof(GetReview), new { id = reviewId });
    }
}