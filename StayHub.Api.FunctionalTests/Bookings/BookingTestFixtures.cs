using System.Net.Http.Json;

namespace StayHub.Api.FunctionalTests.Bookings;

internal static class BookingTestFixtures
{
    internal static async Task<Guid> ReserveAsync(HttpClient httpClient, Guid apartmentId, int startOffsetDays = 10)
    {
        var response = await httpClient.PostAsJsonAsync(
            BookingRoutes.BaseRoute, BookingTestData.ValidReserveRequest(apartmentId, startOffsetDays));

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<Guid>();
    }
}