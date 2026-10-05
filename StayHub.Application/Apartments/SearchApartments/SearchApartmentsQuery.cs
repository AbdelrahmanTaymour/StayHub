using StayHub.Application.Abstractions.Messaging;
using StayHub.Domain.Abstractions;

namespace StayHub.Application.Apartments.SearchApartments;

public sealed record SearchApartmentsQuery(
    string? City = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    DateOnly? Start = null,
    DateOnly? End = null,
    int Page = 1,
    int PageSize = 20) : IQuery<PagedResponse<SearchApartmentsResponse>>;