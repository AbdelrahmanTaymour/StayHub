using StayHub.Domain.Apartments;
using StayHub.Domain.Maintenance;

namespace StayHub.Application.IntegrationTests.Maintenance;

internal static class MaintenanceRequestTestData
{
    public static readonly Title Title = new("Leaking faucet");
    public static readonly Description Description = new("The kitchen faucet is leaking");

    public static MaintenanceRequest Create(Guid apartmentId, Guid? reportedByUserId = null,
        DateTime? createdOnUtc = null)
    {
        return MaintenanceRequest.Create(
            apartmentId,
            reportedByUserId ?? Guid.CreateVersion7(),
            Title,
            Description,
            createdOnUtc ?? DateTime.UtcNow);
    }

    public static MaintenanceRequest CreateAndStart(Guid apartmentId, Guid? reportedByUserId = null)
    {
        var request = Create(apartmentId, reportedByUserId);

        request.Start();

        return request;
    }

    public static MaintenanceRequest CreateStartAndResolve(Guid apartmentId, Guid? reportedByUserId = null)
    {
        var request = CreateAndStart(apartmentId, reportedByUserId);

        request.Resolve(DateTime.UtcNow);

        return request;
    }
}