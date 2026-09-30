namespace StayHub.Application.Apartments.GetApartmentForEdit;

public sealed record ApartmentForEditResponse
{
    public Guid Id { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public bool IsActive { get; init; }

    public required ApartmentPricingResponse Pricing { get; init; }

    public required ApartmentAddressResponse Address { get; init; }
}

public sealed class ApartmentPricingResponse
{
    public string Currency { get; init; } = string.Empty;

    public decimal NightlyRate { get; init; }

    public decimal CleaningFee { get; init; }
}

public sealed class ApartmentAddressResponse
{
    public string Street { get; init; } = string.Empty;

    public string City { get; init; } = string.Empty;

    public string ZipCode { get; init; } = string.Empty;

    public string Country { get; init; } = string.Empty;

    public string State { get; init; } = string.Empty;
}