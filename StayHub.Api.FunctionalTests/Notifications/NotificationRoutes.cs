namespace StayHub.Api.FunctionalTests.Notifications;

internal static class NotificationRoutes
{
    public const string BaseRoute = "api/v1/notifications";

    public static string Get(string query = "") =>
        string.IsNullOrEmpty(query) ? BaseRoute : $"{BaseRoute}?{query}";

    public static string MarkAsRead(Guid notificationId) => $"{BaseRoute}/{notificationId}/read";
}