namespace StayHub.Application.Apartments.SearchApartments;

public sealed record SearchApartmentsResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string Country { get; init; } = string.Empty;
    public decimal PricePerNight { get; init; }
    public decimal? TotalPrice { get; init; }
    public string Currency { get; init; } = string.Empty;
    public string? PrimaryImageUrl { get; init; }
    public double? Rating { get; init; }
    public int ReviewCount { get; init; }
    public bool IsFavorited { get; init; }
}