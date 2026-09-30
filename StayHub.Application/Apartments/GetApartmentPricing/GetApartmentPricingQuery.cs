using StayHub.Application.Abstractions.Messaging;

namespace StayHub.Application.Apartments.GetApartmentPricing;

public sealed record GetApartmentPricingQuery(
    Guid ApartmentId,
    DateOnly Start,
    DateOnly End) : IQuery<ApartmentPricingResponse>;