using StayHub.Domain.Apartments;

namespace StayHub.Api.Controllers.Apartments;

public sealed record CreateApartmentAvailabilityBlockRequest(
    DateOnly Start,
    DateOnly End,
    ApartmentUnavailabilityReason Reason);