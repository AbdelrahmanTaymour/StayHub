using StayHub.Application.Abstractions.Messaging;

namespace StayHub.Application.Apartments.ReorderApartmentImages;

public sealed record ReorderApartmentImagesCommand(
    Guid ApartmentId,
    Guid RequestedByUserId,
    IReadOnlyList<Guid> OrderedImageIds) : ICommand;