using StayHub.Application.Abstractions.Messaging;

namespace StayHub.Application.Apartments.SetAsPrimaryImage;

public sealed record SetAsPrimaryImageCommand(Guid ApartmentId, Guid ImageId) : ICommand;