namespace StayHub.Application.Favorites.GetFavoriteApartments;

public sealed record FavoriteApartmentResponse
{
    public Guid ApartmentId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string Country { get; init; } = string.Empty;
    public decimal PricePerNight { get; init; }
    public string Currency { get; init; } = string.Empty;
    public string? PrimaryImageUrl { get; init; }
    public double? Rating { get; init; }
    public int ReviewCount { get; init; }
}