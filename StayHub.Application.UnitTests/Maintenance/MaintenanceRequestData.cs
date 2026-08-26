using StayHub.Domain.Apartments;
using StayHub.Domain.Maintenance;

namespace StayHub.Application.UnitTests.Maintenance;

internal static class MaintenanceRequestData
{
    public static readonly Title Title = new("Leaking faucet");
    public static readonly Description Description = new("The kitchen faucet is leaking");

    public static MaintenanceRequest Create(Guid apartmentId, Guid? reportedByUserId = null)
    {
        return MaintenanceRequest.Create(apartmentId, reportedByUserId ?? Guid.CreateVersion7(), Title, Description,
            DateTime.UtcNow);
    }

    public static MaintenanceRequest CreateAndStart(Guid apartmentId)
    {
        var request = Create(apartmentId);
        request.Start();
        return request;
    }

    public static MaintenanceRequest CreateStartAndResolve(Guid apartmentId)
    {
        var request = CreateAndStart(apartmentId);
        request.Resolve(DateTime.UtcNow);
        return request;
    }
}