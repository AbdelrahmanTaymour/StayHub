using StayHub.Domain.Apartments;

namespace StayHub.Api.Endpoints.Apartments;

public sealed record CreateApartmentAvailabilityBlockRequest(
    DateOnly Start,
    DateOnly End,
    ApartmentUnavailabilityReason Reason);