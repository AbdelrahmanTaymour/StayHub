using StayHub.Domain.Abstractions;

namespace StayHub.Domain.Maintenance.Events;

public sealed record MaintenanceRequestStaffAssignedDomainEvent(Guid MaintenanceRequestId, Guid StaffId) : IDomainEvent;