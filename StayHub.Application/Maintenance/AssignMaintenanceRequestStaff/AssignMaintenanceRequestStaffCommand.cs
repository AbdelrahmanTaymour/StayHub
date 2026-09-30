using StayHub.Application.Abstractions.Messaging;

namespace StayHub.Application.Maintenance.AssignMaintenanceRequestStaff;

public sealed record AssignMaintenanceRequestStaffCommand(Guid MaintenanceRequestId, Guid StaffUserId) : ICommand;