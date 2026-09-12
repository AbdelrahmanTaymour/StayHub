using System.Net.Http.Json;
using System.Text.Json;

namespace StayHub.Api.FunctionalTests.Infrastructure;

internal static class MailpitClient
{
    /// <summary>
    /// Polls Mailpit until the expected number of messages addressed to recipientEmail appears.
    /// Throws TimeoutException if the expected count is not reached within the window.
    /// </summary>
    public static async Task WaitForMessageCountToAsync(
        FunctionalTestWebAppFactory factory,
        string recipientEmail,
        int expectedCount,
        TimeSpan? timeout = null)
    {
        var effectiveTimeout = timeout ?? TimeSpan.FromSeconds(15);
        using var cts = new CancellationTokenSource(effectiveTimeout);
        using var client = new HttpClient { BaseAddress = factory.MailpitApiBaseAddress };

        var query = Uri.EscapeDataString($"to:\"{recipientEmail}\"");

        while (!cts.Token.IsCancellationRequested)
        {
            try
            {
                var searchResponse = await client.GetAsync(
                    $"api/v1/search?query={query}",
                    cts.Token);

                if (searchResponse.IsSuccessStatusCode)
                {
                    var searchResult =
                        await searchResponse.Content.ReadFromJsonAsync<JsonElement>(cts.Token);

                    if (searchResult.TryGetProperty("messages_count", out var countProperty) &&
                        countProperty.GetInt32() >= expectedCount)
                    {
                        return;
                    }

                    if (searchResult.TryGetProperty("messages", out var messages) &&
                        messages.ValueKind == JsonValueKind.Array &&
                        messages.GetArrayLength() >= expectedCount)
                    {
                        return;
                    }
                }
            }
            catch (OperationCanceledException) when (cts.IsCancellationRequested)
            {
                break;
            }

            await Task.Delay(250, cts.Token).ConfigureAwait(false);
        }

        throw new TimeoutException(
            $"Expected at least {expectedCount} emails for {recipientEmail} " +
            $"within {effectiveTimeout}.");
    }

    /// <summary>
    /// Polls Mailpit until a message addressed to recipientEmail appears, then returns that
    /// message's full body (including Subject/Text/HTML). Throws TimeoutException if nothing
    /// arrives within the window.
    /// </summary>
    public static async Task<JsonElement> WaitForMessageToAsync(
        FunctionalTestWebAppFactory factory,
        string recipientEmail,
        TimeSpan? timeout = null)
    {
        var effectiveTimeout = timeout ?? TimeSpan.FromSeconds(15);
        using var cts = new CancellationTokenSource(effectiveTimeout);
        using var client = new HttpClient { BaseAddress = factory.MailpitApiBaseAddress };

        var query = Uri.EscapeDataString($"to:\"{recipientEmail}\"");

        while (!cts.Token.IsCancellationRequested)
        {
            try
            {
                var searchResponse = await client.GetAsync($"api/v1/search?query={query}", cts.Token);

                if (searchResponse.IsSuccessStatusCode)
                {
                    var searchResult = await searchResponse.Content.ReadFromJsonAsync<JsonElement>(cts.Token);

                    if (searchResult.TryGetProperty("messages", out var messages) &&
                        messages.ValueKind == JsonValueKind.Array &&
                        messages.GetArrayLength() > 0)
                    {
                        var messageId = messages[0].GetProperty("ID").GetString();
                        var messageResponse = await client.GetAsync($"api/v1/message/{messageId}", cts.Token);
                        messageResponse.EnsureSuccessStatusCode();

                        return await messageResponse.Content.ReadFromJsonAsync<JsonElement>(cts.Token);
                    }
                }
            }
            catch (OperationCanceledException) when (cts.IsCancellationRequested)
            {
                break;
            }

            await Task.Delay(250, cts.Token).ConfigureAwait(false);
        }

        throw new TimeoutException(
            $"No email arrived for {recipientEmail} within {effectiveTimeout}.");
    }

    /// <summary>
    /// Confirms nothing arrived for recipientEmail after waiting a reasonable window. Used for
    /// the enumeration-safety test (unregistered email must not trigger a real send).
    /// </summary>
    public static async Task<bool> ConfirmNoMessageToAsync(
        FunctionalTestWebAppFactory factory,
        string recipientEmail,
        TimeSpan waitBeforeChecking)
    {
        await Task.Delay(waitBeforeChecking);

        using var client = new HttpClient { BaseAddress = factory.MailpitApiBaseAddress };
        var query = Uri.EscapeDataString($"to:\"{recipientEmail}\"");

        var searchResponse = await client.GetAsync($"api/v1/search?query={query}");
        searchResponse.EnsureSuccessStatusCode();

        var searchResult = await searchResponse.Content.ReadFromJsonAsync<JsonElement>();

        // Check total count if returned by API
        if (searchResult.TryGetProperty("messages_count", out var countProperty))
        {
            return countProperty.GetInt32() == 0;
        }

        // Fallback: check array presence and length
        if (searchResult.TryGetProperty("messages", out var messagesProperty) &&
            messagesProperty.ValueKind == JsonValueKind.Array)
        {
            return messagesProperty.GetArrayLength() == 0;
        }

        return true;
    }
}