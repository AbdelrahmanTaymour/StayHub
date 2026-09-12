namespace StayHub.Api.FunctionalTests.Maintenance;

internal static class MaintenanceTestData
{
    internal static object ValidCreateRequest(string? title = null, string? description = null)
    {
        return new
        {
            Title = title ?? $"Leaky faucet {Guid.NewGuid():N}",
            Description = description ?? "The kitchen faucet has been dripping constantly for two days."
        };
    }
}