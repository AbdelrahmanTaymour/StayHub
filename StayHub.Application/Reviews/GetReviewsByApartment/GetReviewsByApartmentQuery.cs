using StayHub.Application.Abstractions.Messaging;

namespace StayHub.Application.Reviews.GetReviewsByApartment;

public sealed record GetReviewsByApartmentQuery(Guid ApartmentId, int Page, int PageSize)
    : IQuery<IReadOnlyList<ReviewListItemResponse>>;