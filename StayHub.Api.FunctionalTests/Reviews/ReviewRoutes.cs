namespace StayHub.Api.FunctionalTests.Reviews;

internal static class ReviewRoutes
{
    public const string BaseRoute = "api/v1/reviews";

    public static string ById(Guid id) => $"{BaseRoute}/{id}";

    public static string ByApartment(Guid apartmentId, string query = "") =>
        string.IsNullOrEmpty(query)
            ? $"{BaseRoute}/by-apartment/{apartmentId}"
            : $"{BaseRoute}/by-apartment/{apartmentId}?{query}";

    public static string Response(Guid reviewId) => $"{BaseRoute}/{reviewId}/response";
}