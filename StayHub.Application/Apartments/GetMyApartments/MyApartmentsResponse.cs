namespace StayHub.Application.Apartments.GetMyApartments;

public sealed record MyApartmentsResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string Country { get; init; } = string.Empty;
    public string? PrimaryImageUrl { get; init; }
    public bool IsActive { get; init; }
    public decimal PricePerNight { get; init; }
    public string Currency { get; init; } = string.Empty;
    public double? Rating { get; init; }
    public int ReviewCount { get; init; }
    public double OccupancyRateLast30Days { get; init; } // 0-100
    public int PhotoCount { get; init; }
    public int PendingBookingsCount { get; init; }
}