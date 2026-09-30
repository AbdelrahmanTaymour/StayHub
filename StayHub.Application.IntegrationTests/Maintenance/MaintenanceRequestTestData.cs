using StayHub.Domain.Apartments;
using StayHub.Domain.Maintenance;

namespace StayHub.Application.IntegrationTests.Maintenance;

internal static class MaintenanceRequestTestData
{
    private static readonly Title DefaultTitle = new("Leaking faucet");

    private static readonly Description DefaultDescription =
        new("The kitchen faucet is leaking");

    public static MaintenanceRequest Create(
        Guid apartmentId,
        Guid? reportedByUserId = null,
        string? title = null,
        DateTime? createdOnUtc = null)
    {
        return MaintenanceRequest.Create(
            apartmentId,
            reportedByUserId ?? Guid.CreateVersion7(),
            title is not null ? new Title(title) : DefaultTitle,
            DefaultDescription,
            createdOnUtc ?? DateTime.UtcNow);
    }

    public static MaintenanceRequest CreateAndStart(
        Guid apartmentId,
        Guid? reportedByUserId = null,
        string? title = null,
        DateTime? createdOnUtc = null)
    {
        var request = Create(
            apartmentId,
            reportedByUserId,
            title,
            createdOnUtc);

        request.Start();

        return request;
    }

    public static MaintenanceRequest CreateStartAndResolve(
        Guid apartmentId,
        Guid? reportedByUserId = null)
    {
        var request = CreateAndStart(apartmentId, reportedByUserId);

        request.Resolve(DateTime.UtcNow);

        return request;
    }
}