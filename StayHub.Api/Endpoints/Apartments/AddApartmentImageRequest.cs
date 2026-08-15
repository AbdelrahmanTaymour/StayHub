namespace StayHub.Api.Endpoints.Apartments;

public sealed class AddApartmentImageRequest
{
    public IFormFile File { get; init; }

    public bool IsPrimary { get; init; }
}