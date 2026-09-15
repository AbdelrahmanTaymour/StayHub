using Dapper;
using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Abstractions.Data;
using StayHub.Application.Abstractions.Messaging;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Apartments;
using StayHub.Domain.Maintenance;

namespace StayHub.Application.Maintenance.GetMaintenanceRequestsByApartment;

internal sealed class GetMaintenanceRequestsByApartmentQueryHandler(
    ISqlConnectionFactory sqlConnectionFactory,
    IApartmentRepository apartmentRepository,
    IApartmentStaffAssignmentRepository staffAssignmentRepository,
    IUserContext userContext)
    : IQueryHandler<GetMaintenanceRequestsByApartmentQuery,
        IReadOnlyList<MaintenanceRequestsSummaryResponse>>
{
    public async Task<Result<IReadOnlyList<MaintenanceRequestsSummaryResponse>>> Handle(
        GetMaintenanceRequestsByApartmentQuery request,
        CancellationToken cancellationToken)
    {
        var apartment = await apartmentRepository.GetByIdAsync(request.ApartmentId, cancellationToken);

        if (apartment is null)
            return Result
                .Failure<IReadOnlyList<MaintenanceRequestsSummaryResponse>>(ApartmentErrors.NotFound);

        var isOwner = userContext.IsOwner(apartment.OwnerId);
        var isAdmin = userContext.IsAdmin;
        var isActiveStaff = !isOwner && await staffAssignmentRepository.GetActiveAsync(
            apartment.Id,
            userContext.UserId,
            cancellationToken) is not null;

        if (!isOwner && !isAdmin && !isActiveStaff)
        {
            return Result
                .Failure<IReadOnlyList<MaintenanceRequestsSummaryResponse>>(MaintenanceRequestErrors.NotAuthorized);
        }

        using var connection = sqlConnectionFactory.CreateConnection();

        const string sql = """
                           SELECT
                               mr.id AS Id,
                               mr.title AS Title,
                               mr.status AS Status,
                               mr.created_on_utc AS CreatedOnUtc,
                               mr.reported_by_user_id AS ReportedByUserId,
                               u.first_name AS ReporterFirstName,
                               u.last_name AS ReporterLastName,
                               up.avatar_url AS ReporterAvatarUrl
                           FROM maintenance_requests mr
                           INNER JOIN users u ON u.id = mr.reported_by_user_id
                           LEFT JOIN user_profiles up ON up.user_id = u.id
                           WHERE mr.apartment_id = @ApartmentId
                             AND (@Status IS NULL OR mr.status = @Status)
                           ORDER BY mr.created_on_utc DESC, mr.id DESC
                           OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
                           """;

        var requests = await connection.QueryAsync<MaintenanceRequestsSummaryResponse>(
            sql,
            new
            {
                request.ApartmentId,
                Status = request.Status.HasValue ? (int)request.Status.Value : (int?)null,
                Offset = (request.Page - 1) * request.PageSize,
                request.PageSize
            });

        return requests.ToList();
    }
}