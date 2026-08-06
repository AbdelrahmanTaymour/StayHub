using StayHub.Application.Abstractions.Messaging;
using StayHub.Application.Apartments.GetApartmentsByOwner;

namespace StayHub.Application.Favorites.GetFavoriteApartments;

public sealed record GetFavoriteApartmentsQuery(int Page, int PageSize)
    : IQuery<IReadOnlyList<ApartmentSummaryResponse>>;