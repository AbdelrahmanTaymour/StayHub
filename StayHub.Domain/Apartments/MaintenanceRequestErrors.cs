using StayHub.Domain.Abstractions;

namespace StayHub.Domain.Apartments;

public static class MaintenanceRequestErrors
{
    public static Error NotFound = new(
        "MaintenanceRequest.NotFound",
        "The maintenance request with the specified identifier was not found");

    public static Error NotOpen = new(
        "MaintenanceRequest.NotOpen",
        "The maintenance request is not open");

    public static Error NotInProgress = new(
        "MaintenanceRequest.NotInProgress",
        "The maintenance request is not in progress");

    public static Error NotResolved = new(
        "MaintenanceRequest.NotResolved",
        "The maintenance request is not resolved");
}