using StayHub.Application.Abstractions.Messaging;

namespace StayHub.Application.Apartments.GetApartmentAvailabilityBlocks;

public sealed record GetApartmentAvailabilityBlocksQuery(
    Guid ApartmentId,
    int? Year,
    int? Month) : IQuery<ApartmentAvailabilityResponse>;