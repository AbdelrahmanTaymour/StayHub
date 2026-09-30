using Dapper;
using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Abstractions.Data;
using StayHub.Application.Abstractions.Messaging;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Apartments;
using StayHub.Domain.Maintenance;

namespace StayHub.Application.Maintenance.GetApartmentMaintenanceRequests;

internal sealed class GetApartmentMaintenanceRequestsQueryHandler(
    ISqlConnectionFactory sqlConnectionFactory,
    IApartmentRepository apartmentRepository,
    IApartmentStaffAssignmentRepository staffAssignmentRepository,
    IUserContext userContext)
    : IQueryHandler<GetApartmentMaintenanceRequestsQuery,
        PagedResponse<MaintenanceRequestsResponse>>
{
    public async Task<Result<PagedResponse<MaintenanceRequestsResponse>>> Handle(
        GetApartmentMaintenanceRequestsQuery request,
        CancellationToken cancellationToken)
    {
        var apartment = await apartmentRepository.GetByIdAsync(request.ApartmentId, cancellationToken);

        if (apartment is null)
            return Result
                .Failure<PagedResponse<MaintenanceRequestsResponse>>(ApartmentErrors.NotFound);

        var isOwner = userContext.IsOwner(apartment.OwnerId);
        var isAdmin = userContext.IsAdmin;
        var isActiveStaff = !isOwner && await staffAssignmentRepository.GetActiveAsync(
            apartment.Id,
            userContext.UserId,
            cancellationToken) is not null;

        if (!isOwner && !isAdmin && !isActiveStaff)
        {
            return Result
                .Failure<PagedResponse<MaintenanceRequestsResponse>>(MaintenanceRequestErrors.NotAuthorized);
        }

        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize switch
        {
            < 1 => 10,
            > 50 => 50,
            _ => request.PageSize
        };

        var search = string.IsNullOrWhiteSpace(request.Search)
            ? null
            : $"%{EscapeLike(request.Search.Trim())}%";

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
                               up.avatar_url AS ReporterAvatarUrl,
                               COUNT(*) OVER() AS TotalCount
                           FROM maintenance_requests mr
                           INNER JOIN users u ON u.id = mr.reported_by_user_id
                           LEFT JOIN user_profiles up ON up.user_id = u.id
                           WHERE mr.apartment_id = @ApartmentId
                             AND (@Status IS NULL OR mr.status = @Status)
                             AND (@Search IS NULL
                                  OR mr.title ILIKE @Search
                                  OR (u.first_name || ' ' || u.last_name) ILIKE @Search)
                           ORDER BY mr.created_on_utc DESC, mr.id DESC
                           OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
                           """;

        var rows = (await connection.QueryAsync<MaintenanceRequestRow>(
            sql,
            new
            {
                request.ApartmentId,
                Status = request.Status.HasValue ? (int)request.Status.Value : (int?)null,
                Search = search,
                Offset = (page - 1) * pageSize,
                PageSize = pageSize
            })).ToList();

        if (rows.Count == 0)
        {
            return new PagedResponse<MaintenanceRequestsResponse>
            {
                Items = [],
                Page = page,
                PageSize = pageSize,
                TotalCount = 0,
                TotalPages = 0
            };
        }

        var items = rows.Select(r => new MaintenanceRequestsResponse
        {
            Id = r.Id,
            Title = r.Title,
            Status = r.Status,
            CreatedOnUtc = r.CreatedOnUtc,
            ReportedByUserId = r.ReportedByUserId,
            ReporterFirstName = r.ReporterFirstName,
            ReporterLastName = r.ReporterLastName,
            ReporterAvatarUrl = r.ReporterAvatarUrl,
            IsReportedByOwner = r.ReportedByUserId == apartment.OwnerId
        }).ToList();

        var totalCount = rows[0].TotalCount;
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        return new PagedResponse<MaintenanceRequestsResponse>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages
        };
    }

    // Escapes LIKE wildcards so a user typing "%" or "_" searches for the literal character.
    private static string EscapeLike(string value) =>
        value.Replace("\\", @"\\").Replace("%", "\\%").Replace("_", "\\_");

    private sealed class MaintenanceRequestRow
    {
        public Guid Id { get; init; }
        public string Title { get; init; } = string.Empty;
        public MaintenanceRequestStatus Status { get; init; }
        public DateTime CreatedOnUtc { get; init; }
        public Guid ReportedByUserId { get; init; }
        public string ReporterFirstName { get; init; } = string.Empty;
        public string ReporterLastName { get; init; } = string.Empty;
        public string? ReporterAvatarUrl { get; init; }
        public int TotalCount { get; init; }
    }
}