using StayHub.Application.Abstractions.Messaging;

namespace StayHub.Application.Apartments.ActivateApartment;

public sealed record ActivateApartmentCommand(Guid ApartmentId, Guid RequestedByUserId) : ICommand;