namespace StayHub.Api.FunctionalTests.Bookings;

internal static class BookingRoutes
{
    public const string BaseRoute = "api/v1/bookings";

    public static string ById(Guid id) => $"{BaseRoute}/{id}";

    public static string Mine(string query = "") =>
        string.IsNullOrEmpty(query) ? $"{BaseRoute}/mine" : $"{BaseRoute}/mine?{query}";

    public static string ByUser(Guid userId, string query = "") =>
        string.IsNullOrEmpty(query) ? $"{BaseRoute}/by-user/{userId}" : $"{BaseRoute}/by-user/{userId}?{query}";

    public static string ByApartment(Guid apartmentId, string query = "") =>
        string.IsNullOrEmpty(query)
            ? $"{BaseRoute}/by-apartment/{apartmentId}"
            : $"{BaseRoute}/by-apartment/{apartmentId}?{query}";

    public static string Confirm(Guid id) => $"{BaseRoute}/{id}/confirm";
    public static string Reject(Guid id) => $"{BaseRoute}/{id}/reject";
    public static string Cancel(Guid id) => $"{BaseRoute}/{id}/cancel";
}