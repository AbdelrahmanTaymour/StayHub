using MediatR;
using Microsoft.AspNetCore.Mvc;
using StayHub.Api.Extensions;
using StayHub.Application.Apartments.GetApartmentsByOwner;
using StayHub.Application.Favorites.AddFavoriteApartment;
using StayHub.Application.Favorites.GetFavoriteApartments;
using StayHub.Application.Favorites.RemoveFavoriteApartment;

namespace StayHub.Api.Controllers.Favorites;

[ApiController]
[Route("api/v{version:apiVersion}/users/{userId:guid}/favorites")]
public sealed class FavoritesController(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ApartmentSummaryResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ApartmentSummaryResponse>>> Get(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = new GetFavoriteApartmentsQuery(page, pageSize);

        var result = await sender.Send(query, cancellationToken);

        return result.IsFailure ? result.ToProblemDetails(this) : Ok(result.Value);
    }

    [HttpPut("{apartmentId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult> Add(Guid apartmentId, CancellationToken cancellationToken)
    {
        var command = new AddFavoriteApartmentCommand(apartmentId);

        var result = await sender.Send(command, cancellationToken);

        return result.IsFailure ? result.ToProblemDetails(this) : NoContent();
    }

    [HttpDelete("{apartmentId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Remove(Guid apartmentId, CancellationToken cancellationToken)
    {
        var command = new RemoveFavoriteApartmentCommand(apartmentId);

        var result = await sender.Send(command, cancellationToken);

        return result.IsFailure ? result.ToProblemDetails(this) : NoContent();
    }
}