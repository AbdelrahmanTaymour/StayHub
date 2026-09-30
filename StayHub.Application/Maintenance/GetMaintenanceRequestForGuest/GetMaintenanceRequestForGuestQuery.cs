using StayHub.Application.Abstractions.Messaging;

namespace StayHub.Application.Maintenance.GetMaintenanceRequestForGuest;

public sealed record GetMaintenanceRequestForGuestQuery(Guid MaintenanceRequestId)
    : IQuery<GuestMaintenanceRequestResponse>;