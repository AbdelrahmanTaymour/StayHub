using MediatR;
using StayHub.Api.Extensions;
using StayHub.Application.Favorites.AddFavoriteApartment;
using StayHub.Application.Favorites.GetFavoriteApartments;
using StayHub.Application.Favorites.RemoveFavoriteApartment;
using StayHub.Domain.Abstractions;

namespace StayHub.Api.Endpoints.Favorites;

public static class FavoriteEndpoints
{
    public static IEndpointRouteBuilder MapFavoriteEndpoints(this IEndpointRouteBuilder builder)
    {
        var group = builder.MapGroup("favorites").WithTags("Favorites").RequireAuthorization();

        group.MapGet("", Get)
            .Produces<PagedResponse<FavoriteApartmentResponse>>();

        group.MapPut("{apartmentId:guid}", Add)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapDelete("{apartmentId:guid}", Remove)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return builder;
    }

    private static async Task<IResult> Get(
        ISender sender,
        CancellationToken cancellationToken,
        int page = 1,
        int pageSize = 12)
    {
        var result = await sender.Send(new GetFavoriteApartmentsQuery(page, pageSize), cancellationToken);

        return result.IsFailure ? result.ToProblemDetails() : TypedResults.Ok(result.Value);
    }

    private static async Task<IResult> Add(Guid apartmentId, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new AddFavoriteApartmentCommand(apartmentId), cancellationToken);

        return result.IsFailure ? result.ToProblemDetails() : Results.NoContent();
    }

    private static async Task<IResult> Remove(Guid apartmentId, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RemoveFavoriteApartmentCommand(apartmentId), cancellationToken);

        return result.IsFailure ? result.ToProblemDetails() : Results.NoContent();
    }
}