using StayHub.Application.Abstractions.Messaging;
using StayHub.Application.Apartments.GetApartmentsByOwner;

namespace StayHub.Application.Apartments.SearchApartments;

public sealed record SearchApartmentsQuery(
    string? City,
    decimal? MinPrice,
    decimal? MaxPrice,
    DateOnly? Start,
    DateOnly? End,
    int Page,
    int PageSize) : IQuery<IReadOnlyList<ApartmentSummaryResponse>>;