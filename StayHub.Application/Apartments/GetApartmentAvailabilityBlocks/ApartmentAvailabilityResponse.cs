namespace StayHub.Application.Apartments.GetApartmentAvailabilityBlocks;

public sealed class ApartmentAvailabilityResponse
{
    public List<AvailabilityBlockResponse> Blocks { get; init; } = [];
    public List<BookedRangeResponse> BookedRanges { get; init; } = [];
}

public sealed record AvailabilityBlockResponse(
    Guid Id,
    DateOnly StartDate,
    DateOnly EndDate,
    string? Reason); // null when the caller isn't the owner/admin

public sealed record BookedRangeResponse(
    Guid BookingId,
    DateOnly StartDate,
    DateOnly EndDate);