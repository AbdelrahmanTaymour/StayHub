namespace StayHub.Api.FunctionalTests.Bookings;

internal static class BookingTestData
{
    internal static object ValidReserveRequest(Guid apartmentId, int startOffsetDays = 10, int durationDays = 3)
    {
        var start = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(startOffsetDays));
        var end = start.AddDays(durationDays);

        return new { ApartmentId = apartmentId, StartDate = start, EndDate = end };
    }
}