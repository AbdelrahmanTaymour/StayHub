using StayHub.Domain.Apartments;
using StayHub.Domain.Maintenance;

namespace StayHub.Domain.UnitTests.Maintenance;

internal static class MaintenanceRequestData
{
    public static readonly Title Title = new("Leaking faucet");
    public static readonly Description Description = new("The kitchen faucet is leaking");

    public static MaintenanceRequest Create(Guid? apartmentId = null, Guid? reportedByUserId = null)
    {
        return MaintenanceRequest.Create(
            apartmentId ?? Guid.CreateVersion7(),
            reportedByUserId ?? Guid.CreateVersion7(),
            Title,
            Description,
            DateTime.UtcNow);
    }

    public static MaintenanceRequest CreateAndStart()
    {
        var request = Create();
        request.Start();
        return request;
    }

    public static MaintenanceRequest CreateStartAndResolve()
    {
        var request = CreateAndStart();
        request.Resolve(DateTime.UtcNow);
        return request;
    }
}