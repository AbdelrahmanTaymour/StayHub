namespace StayHub.Api.FunctionalTests.Conversations;

internal static class ConversationRoutes
{
    public const string BaseRoute = "api/v1/conversations";

    public static string Messages(Guid id, string query = "") =>
        string.IsNullOrEmpty(query) ? $"{BaseRoute}/{id}/messages" : $"{BaseRoute}/{id}/messages?{query}";

    public static string MarkAsRead(Guid id) => $"{BaseRoute}/{id}/read";
}