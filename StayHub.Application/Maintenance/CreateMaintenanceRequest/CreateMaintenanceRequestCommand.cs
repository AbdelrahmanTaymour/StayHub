using StayHub.Application.Abstractions.Messaging;

namespace StayHub.Application.Maintenance.CreateMaintenanceRequest;

public sealed record CreateMaintenanceRequestCommand(
    Guid ApartmentId,
    Guid ReportedByUserId,
    string Title,
    string Description) : ICommand<Guid>;