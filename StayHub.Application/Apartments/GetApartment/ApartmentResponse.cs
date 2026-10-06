namespace StayHub.Application.Apartments.GetApartment;

public sealed record ApartmentResponse
{
    public Guid Id { get; init; }

    public Guid OwnerId { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public required AddressResponse Address { get; set; }

    public decimal PriceAmount { get; init; }

    public string PriceCurrency { get; init; } = string.Empty;

    public decimal CleaningFeeAmount { get; init; }

    public string CleaningFeeCurrency { get; init; } = string.Empty;

    public bool IsActive { get; init; }

    public IReadOnlyList<string> Amenities { get; set; } = [];

    public IReadOnlyList<ApartmentImageResponse> Images { get; set; } = [];

    public double? Rating { get; init; }

    public int ReviewCount { get; init; }

    public required bool IsFavorited { get; init; }

    public required ApartmentHostResponse Host { get; set; }

    public IReadOnlyList<ApartmentReviewPreviewResponse> RecentReviews { get; set; } = [];
}

public sealed class ApartmentHostResponse
{
    public Guid Id { get; init; }

    public string FullName { get; init; } = string.Empty;

    public string? AvatarUrl { get; init; }
}

public sealed class ApartmentReviewPreviewResponse
{
    public Guid Id { get; init; }

    public string ReviewerName { get; init; } = string.Empty;

    public string? ReviewerAvatarUrl { get; init; }

    public int Rating { get; init; }

    public string Comment { get; init; } = string.Empty;

    public DateTime CreatedOnUtc { get; init; }
}

public sealed class ApartmentImageResponse
{
    public Guid Id { get; init; }

    public string Url { get; init; } = string.Empty;

    public int DisplayOrder { get; init; }

    public bool IsPrimary { get; init; }
}

public sealed class AddressResponse
{
    public string Country { get; init; }

    public string State { get; init; }

    public string ZipCode { get; init; }

    public string City { get; init; }

    public string Street { get; init; }
}