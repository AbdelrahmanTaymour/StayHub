using StayHub.Application.Abstractions.Messaging;
using StayHub.Domain.Abstractions;

namespace StayHub.Application.Favorites.GetFavoriteApartments;

public sealed record GetFavoriteApartmentsQuery(
    int Page = 1,
    int PageSize = 12) : IQuery<PagedResponse<FavoriteApartmentResponse>>;