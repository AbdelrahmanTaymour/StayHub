namespace StayHub.Api.Endpoints.Apartments;

public sealed record ReorderApartmentImagesRequest(IReadOnlyList<Guid> OrderedImageIds);