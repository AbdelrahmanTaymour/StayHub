using Dapper;
using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Abstractions.Data;
using StayHub.Application.Abstractions.Messaging;
using StayHub.Application.Abstractions.Storage;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Apartments;

namespace StayHub.Application.Apartments.GetApartmentStaff;

internal sealed class GetApartmentStaffQueryHandler(
    ISqlConnectionFactory sqlConnectionFactory,
    IUserContext userContext,
    IFileStorageService fileStorageService)
    : IQueryHandler<GetApartmentStaffQuery, IReadOnlyList<ApartmentStaffResponse>>
{
    public async Task<Result<IReadOnlyList<ApartmentStaffResponse>>> Handle(
        GetApartmentStaffQuery request,
        CancellationToken cancellationToken)
    {
        using var connection = sqlConnectionFactory.CreateConnection();

        const string sql = """
                           SELECT owner_id FROM apartments WHERE id = @ApartmentId;

                           SELECT
                               asa.id AS AssignmentId,
                               u.id AS UserId,
                               u.first_name || ' ' || u.last_name AS FullName,
                               p.avatar_key AS AvatarKey,
                               p.phone_number AS PhoneNumber,
                               asa.role AS Role,
                               asa.created_on_utc AS AssignedOnUtc
                           FROM apartment_staff_assignments asa
                           JOIN users u ON u.id = asa.user_id
                           LEFT JOIN user_profiles p ON p.user_id = u.id
                           WHERE asa.apartment_id = @ApartmentId
                             AND asa.revoked_on_utc IS NULL
                           ORDER BY asa.created_on_utc ASC;
                           """;

        using var multi = await connection.QueryMultipleAsync(sql, new { request.ApartmentId });

        var ownerId = await multi.ReadFirstOrDefaultAsync<Guid?>();

        if (ownerId is null || !userContext.IsOwner(ownerId.Value) && !userContext.IsAdmin)
        {
            return Result.Failure<IReadOnlyList<ApartmentStaffResponse>>(ApartmentErrors.NotFound);
        }

        var staffRows = (await multi.ReadAsync<ApartmentStaffRow>()).ToList();

        var staff = await ToStaffResponsesAsync(staffRows, cancellationToken);

        return staff.ToList();
    }

    internal async Task<IReadOnlyList<ApartmentStaffResponse>> ToStaffResponsesAsync(
        IReadOnlyList<ApartmentStaffRow> rows,
        CancellationToken cancellationToken)
    {
        var tasks = rows.Select(async row =>
        {
            var avatarUrl = string.IsNullOrWhiteSpace(row.AvatarKey)
                ? null
                : await fileStorageService.GeneratePresignedUrlAsync(row.AvatarKey, cancellationToken);

            return new ApartmentStaffResponse
            {
                AssignmentId = row.AssignmentId,
                UserId = row.UserId,
                FullName = row.FullName,
                AvatarUrl = avatarUrl,
                PhoneNumber = row.PhoneNumber,
                Role = row.Role,
                AssignedOnUtc = row.AssignedOnUtc
            };
        });

        return (await Task.WhenAll(tasks)).ToList();
    }

    internal sealed class ApartmentStaffRow
    {
        public Guid AssignmentId { get; init; }
        public Guid UserId { get; init; }
        public string FullName { get; init; } = string.Empty;
        public string? AvatarKey { get; init; }
        public string? PhoneNumber { get; init; }
        public ApartmentStaffRole Role { get; init; }
        public DateTime AssignedOnUtc { get; init; }
    }
}