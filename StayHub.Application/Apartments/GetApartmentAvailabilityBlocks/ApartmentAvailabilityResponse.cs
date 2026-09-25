namespace StayHub.Application.Apartments.GetApartmentAvailabilityBlocks;

public sealed class ApartmentAvailabilityResponse
{
    public List<AvailabilityBlockResponse> Blocks { get; init; } = [];
}

public sealed record AvailabilityBlockResponse(
    Guid Id,
    DateOnly StartDate,
    DateOnly EndDate,
    string Reason);