using StayHub.Domain.Abstractions;

namespace StayHub.Domain.Maintenance;

public static class MaintenanceRequestErrors
{
    public static readonly Error NotFound = Error.NotFound(
        "MaintenanceRequest.NotFound",
        "The maintenance request with the specified identifier was not found");

    public static readonly Error NotOpen = Error.Conflict(
        "MaintenanceRequest.NotOpen",
        "The maintenance request is not open");

    public static readonly Error NotInProgress = Error.Conflict(
        "MaintenanceRequest.NotInProgress",
        "The maintenance request is not in progress");

    public static readonly Error NotResolved = Error.Conflict(
        "MaintenanceRequest.NotResolved",
        "The maintenance request is not resolved");

    public static readonly Error NotAuthorized = Error.Forbidden(
        "MaintenanceRequest.NotAuthorized",
        "You're not authorized to perform this action"
    );
}