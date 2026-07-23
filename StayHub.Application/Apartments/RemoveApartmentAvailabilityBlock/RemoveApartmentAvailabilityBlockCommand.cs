using StayHub.Application.Abstractions.Messaging;

namespace StayHub.Application.Apartments.RemoveApartmentAvailabilityBlock;

public sealed record RemoveApartmentAvailabilityBlockCommand(Guid BlockId, Guid RequestedByUserId) : ICommand;