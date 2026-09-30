using Dapper;
using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Abstractions.Data;
using StayHub.Application.Abstractions.Messaging;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Maintenance;

namespace StayHub.Application.Maintenance.GetMaintenanceRequestForGuest;

internal sealed class GetMaintenanceRequestForGuestQueryHandler(
    ISqlConnectionFactory sqlConnectionFactory,
    IMaintenanceRequestRepository maintenanceRequestRepository,
    IUserContext userContext) : IQueryHandler<GetMaintenanceRequestForGuestQuery, GuestMaintenanceRequestResponse>
{
    public async Task<Result<GuestMaintenanceRequestResponse>> Handle(
        GetMaintenanceRequestForGuestQuery request,
        CancellationToken cancellationToken)
    {
        var maintenanceRequest = await maintenanceRequestRepository.GetByIdAsync(
            request.MaintenanceRequestId,
            cancellationToken);

        if (maintenanceRequest is null)
            return Result.Failure<GuestMaintenanceRequestResponse>(MaintenanceRequestErrors.NotFound);

        if (maintenanceRequest.ReportedByUserId != userContext.UserId)
        {
            return Result.Failure<GuestMaintenanceRequestResponse>(MaintenanceRequestErrors.NotAuthorized);
        }

        using var connection = sqlConnectionFactory.CreateConnection();

        const string sql = """
                           SELECT
                               mr.id AS Id,
                               mr.apartment_id AS ApartmentId,
                               a.name AS ApartmentName,
                               a.address_city AS ApartmentCity,
                               mr.title AS Title,
                               mr.description AS Description,
                               mr.status AS Status,
                               mr.created_on_utc AS CreatedOnUtc,
                               mr.start_on_utc AS StartOnUtc,
                               mr.resolved_on_utc AS ResolvedOnUtc,
                               mr.closed_on_utc AS ClosedOnUtc
                           FROM maintenance_requests mr
                           INNER JOIN apartments a ON a.id = mr.apartment_id
                           WHERE mr.id = @MaintenanceRequestId
                           """;

        var response = await connection.QueryFirstOrDefaultAsync<GuestMaintenanceRequestResponse>(
            sql,
            new { request.MaintenanceRequestId });

        return response ?? Result.Failure<GuestMaintenanceRequestResponse>(MaintenanceRequestErrors.NotFound);
    }
}