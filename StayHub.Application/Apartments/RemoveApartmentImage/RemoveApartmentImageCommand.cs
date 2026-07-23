using StayHub.Application.Abstractions.Messaging;

namespace StayHub.Application.Apartments.RemoveApartmentImage;

public sealed record RemoveApartmentImageCommand(Guid ImageId, Guid RequestedByUserId) : ICommand;