using Dapper;
using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Abstractions.Data;
using StayHub.Application.Abstractions.Messaging;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Apartments;
using StayHub.Domain.Maintenance;

namespace StayHub.Application.Maintenance.GetMaintenanceRequest;

internal sealed class GetMaintenanceRequestQueryHandler(
    ISqlConnectionFactory sqlConnectionFactory,
    IMaintenanceRequestRepository maintenanceRequestRepository,
    IApartmentRepository apartmentRepository,
    IApartmentStaffAssignmentRepository staffAssignmentRepository,
    IUserContext userContext) : IQueryHandler<GetMaintenanceRequestQuery, MaintenanceRequestResponse>
{
    public async Task<Result<MaintenanceRequestResponse>> Handle(
        GetMaintenanceRequestQuery request,
        CancellationToken cancellationToken)
    {
        var maintenanceRequest = await maintenanceRequestRepository.GetByIdAsync(
            request.MaintenanceRequestId,
            cancellationToken);

        if (maintenanceRequest is null)
            return Result.Failure<MaintenanceRequestResponse>(MaintenanceRequestErrors.NotFound);

        var apartment = await apartmentRepository.GetByIdAsync(maintenanceRequest.ApartmentId, cancellationToken);

        if (apartment is null) return Result.Failure<MaintenanceRequestResponse>(ApartmentErrors.NotFound);

        var isOwner = userContext.IsOwner(apartment.OwnerId);
        var isAdmin = userContext.IsAdmin;
        var isReporter = maintenanceRequest.ReportedByUserId == userContext.UserId;
        var isActiveStaff = !isOwner && await staffAssignmentRepository.GetActiveAsync(
            apartment.Id,
            userContext.UserId,
            cancellationToken) is not null;

        if (!isOwner && !isAdmin && !isReporter && !isActiveStaff)
        {
            return Result.Failure<MaintenanceRequestResponse>(MaintenanceRequestErrors.NotAuthorized);
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
                               mr.resolved_on_utc AS ResolvedOnUtc,
                               mr.closed_on_utc AS ClosedOnUtc,
                               mr.reported_by_user_id AS ReportedByUserId,
                               u.first_name AS ReporterFirstName,
                               u.last_name AS ReporterLastName,
                               u.email AS ReporterEmail,
                               up.phone_number AS ReporterPhoneNumber,
                               up.avatar_url AS ReporterAvatarUrl
                           FROM maintenance_requests mr
                           INNER JOIN apartments a ON a.id = mr.apartment_id
                           INNER JOIN users u ON u.id = mr.reported_by_user_id
                           LEFT JOIN user_profiles up ON up.user_id = u.id
                           WHERE mr.id = @MaintenanceRequestId
                           """;

        var response = await connection.QueryFirstOrDefaultAsync<MaintenanceRequestResponse>(
            sql,
            new { request.MaintenanceRequestId });

        return response ?? Result.Failure<MaintenanceRequestResponse>(MaintenanceRequestErrors.NotFound);
    }
}