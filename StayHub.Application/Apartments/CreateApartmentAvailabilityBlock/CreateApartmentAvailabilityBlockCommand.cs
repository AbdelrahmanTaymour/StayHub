using StayHub.Application.Abstractions.Messaging;
using StayHub.Domain.Apartments;

namespace StayHub.Application.Apartments.CreateApartmentAvailabilityBlock;

public sealed record CreateApartmentAvailabilityBlockCommand(
    Guid ApartmentId,
    DateOnly Start,
    DateOnly End,
    ApartmentUnavailabilityReason Reason) : ICommand<Guid>;