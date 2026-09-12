namespace StayHub.Api.FunctionalTests.Favorites;

internal static class FavoriteRoutes
{
    public const string BaseRoute = "api/v1/favorites";

    public static string Get(string query = "") =>
        string.IsNullOrEmpty(query) ? BaseRoute : $"{BaseRoute}?{query}";

    public static string ById(Guid apartmentId) => $"{BaseRoute}/{apartmentId}";
}