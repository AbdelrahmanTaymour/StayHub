using Dapper;
using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Abstractions.Data;
using StayHub.Application.Abstractions.Messaging;
using StayHub.Application.Abstractions.Storage;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Apartments;
using StayHub.Domain.Maintenance;

namespace StayHub.Application.Maintenance.GetMaintenanceRequest;

internal sealed class GetMaintenanceRequestQueryHandler(
    ISqlConnectionFactory sqlConnectionFactory,
    IMaintenanceRequestRepository maintenanceRequestRepository,
    IApartmentRepository apartmentRepository,
    IApartmentStaffAssignmentRepository staffAssignmentRepository,
    IUserContext userContext,
    IFileStorageService fileStorageService) : IQueryHandler<GetMaintenanceRequestQuery, MaintenanceRequestResponse>
{
    public async Task<Result<MaintenanceRequestResponse>> Handle(
        GetMaintenanceRequestQuery request,
        CancellationToken cancellationToken)
    {
        var maintenanceRequest = await maintenanceRequestRepository.GetByIdAsync(
            request.MaintenanceRequestId,
            cancellationToken);

        if (maintenanceRequest is null)
        {
            return Result.Failure<MaintenanceRequestResponse>(MaintenanceRequestErrors.NotFound);
        }

        var apartment = await apartmentRepository.GetByIdAsync(maintenanceRequest.ApartmentId, cancellationToken);

        if (apartment is null)
        {
            return Result.Failure<MaintenanceRequestResponse>(ApartmentErrors.NotFound);
        }

        var isOwner = userContext.IsOwner(apartment.OwnerId);
        var isAdmin = userContext.IsAdmin;
        var isActiveStaff = !isOwner && await staffAssignmentRepository.GetActiveAsync(
            apartment.Id,
            userContext.UserId,
            cancellationToken) is not null;

        if (!isOwner && !isAdmin && !isActiveStaff)
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
                               mr.start_on_utc AS StartOnUtc,
                               mr.resolved_on_utc AS ResolvedOnUtc,
                               mr.closed_on_utc AS ClosedOnUtc,
                               mr.reported_by_user_id AS ReportedByUserId,
                               u.first_name AS ReporterFirstName,
                               u.last_name AS ReporterLastName,
                               u.email AS ReporterEmail,
                               up.phone_number AS ReporterPhoneNumber,
                               up.avatar_key AS ReporterAvatarKey,
                               mr.assigned_to_user_id AS AssignedToUserId
                           FROM maintenance_requests mr
                           INNER JOIN apartments a ON a.id = mr.apartment_id
                           INNER JOIN users u ON u.id = mr.reported_by_user_id
                           LEFT JOIN user_profiles up ON up.user_id = u.id
                           WHERE mr.id = @MaintenanceRequestId
                           """;

        var row = await connection.QueryFirstOrDefaultAsync<MaintenanceRequestRow>(
            sql,
            new { request.MaintenanceRequestId });

        if (row is null)
        {
            return Result.Failure<MaintenanceRequestResponse>(MaintenanceRequestErrors.NotFound);
        }

        return await ToMaintenanceRequestResponseAsync(row, cancellationToken);
    }

    private async Task<MaintenanceRequestResponse> ToMaintenanceRequestResponseAsync(
        MaintenanceRequestRow row,
        CancellationToken cancellationToken)
    {
        var reporterAvatarUrl = string.IsNullOrWhiteSpace(row.ReporterAvatarKey)
            ? null
            : await fileStorageService.GeneratePresignedUrlAsync(row.ReporterAvatarKey, cancellationToken);

        return new MaintenanceRequestResponse
        {
            Id = row.Id,
            ApartmentId = row.ApartmentId,
            ApartmentName = row.ApartmentName,
            ApartmentCity = row.ApartmentCity,
            Title = row.Title,
            Description = row.Description,
            Status = row.Status,
            CreatedOnUtc = row.CreatedOnUtc,
            StartOnUtc = row.StartOnUtc,
            ResolvedOnUtc = row.ResolvedOnUtc,
            ClosedOnUtc = row.ClosedOnUtc,
            ReportedByUserId = row.ReportedByUserId,
            ReporterFirstName = row.ReporterFirstName,
            ReporterLastName = row.ReporterLastName,
            ReporterEmail = row.ReporterEmail,
            ReporterPhoneNumber = row.ReporterPhoneNumber,
            ReporterAvatarUrl = reporterAvatarUrl,
            AssignedToUserId = row.AssignedToUserId
        };
    }

    internal sealed class MaintenanceRequestRow
    {
        public Guid Id { get; init; }
        public Guid ApartmentId { get; init; }
        public string ApartmentName { get; init; } = string.Empty;
        public string ApartmentCity { get; init; } = string.Empty;
        public string Title { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public MaintenanceRequestStatus Status { get; init; }
        public DateTime CreatedOnUtc { get; init; }
        public DateTime? StartOnUtc { get; init; }
        public DateTime? ResolvedOnUtc { get; init; }
        public DateTime? ClosedOnUtc { get; init; }
        public Guid ReportedByUserId { get; init; }
        public string ReporterFirstName { get; init; } = string.Empty;
        public string ReporterLastName { get; init; } = string.Empty;
        public string ReporterEmail { get; init; } = string.Empty;
        public string? ReporterPhoneNumber { get; init; }
        public string? ReporterAvatarKey { get; init; }
        public Guid? AssignedToUserId { get; init; }
    }
}