using StayHub.Application.Abstractions.Messaging;

namespace StayHub.Application.Apartments.ReorderApartmentImages;

public sealed record ReorderApartmentImagesCommand(
    Guid ApartmentId,
    IReadOnlyList<Guid> OrderedImageIds) : ICommand;