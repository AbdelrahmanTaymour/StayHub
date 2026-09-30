namespace StayHub.Application.Apartments.GetApartmentImages;

public sealed record ApartmentImagesResponse
{
    public IReadOnlyList<ApartmentImageResponse> Photos { get; init; } = [];
}

public sealed class ApartmentImageResponse
{
    public Guid Id { get; init; }

    public string Url { get; init; } = string.Empty;

    public int DisplayOrder { get; init; }

    public bool IsPrimary { get; init; }
}