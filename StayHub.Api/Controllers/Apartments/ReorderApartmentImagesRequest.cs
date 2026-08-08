namespace StayHub.Api.Controllers.Apartments;

public sealed record ReorderApartmentImagesRequest(IReadOnlyList<Guid> OrderedImageIds);