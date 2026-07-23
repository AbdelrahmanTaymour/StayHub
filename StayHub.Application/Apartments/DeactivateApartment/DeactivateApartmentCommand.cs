using StayHub.Application.Abstractions.Messaging;

namespace StayHub.Application.Apartments.DeactivateApartment;

public sealed record DeactivateApartmentCommand(Guid ApartmentId, Guid RequestedByUserId) : ICommand;