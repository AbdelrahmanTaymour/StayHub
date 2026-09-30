using Dapper;
using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Abstractions.Data;
using StayHub.Application.Abstractions.Messaging;
using StayHub.Application.Abstractions.Storage;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Apartments;
using StayHub.Domain.Users;

namespace StayHub.Application.Apartments.SearchStaffCandidate;

internal sealed class SearchStaffCandidateQueryHandler(
    ISqlConnectionFactory sqlConnectionFactory,
    IUserContext userContext,
    IFileStorageService fileStorageService)
    : IQueryHandler<SearchStaffCandidateQuery, StaffCandidateResponse>
{
    public async Task<Result<StaffCandidateResponse>> Handle(
        SearchStaffCandidateQuery request,
        CancellationToken cancellationToken)
    {
        using var connection = sqlConnectionFactory.CreateConnection();

        const string apartmentSql = """
                                    SELECT owner_id
                                    FROM apartments
                                    WHERE id = @ApartmentId
                                    """;

        var ownerId = await connection.ExecuteScalarAsync<Guid?>(apartmentSql, new { request.ApartmentId });

        if (ownerId is null)
        {
            return Result.Failure<StaffCandidateResponse>(ApartmentErrors.NotFound);
        }

        if (!userContext.IsOwner(ownerId.Value) && !userContext.IsAdmin)
        {
            return Result.Failure<StaffCandidateResponse>(ApartmentErrors.NotFound);
        }

        const string userSql = """
                               SELECT
                                   u.id AS UserId,
                                   u.first_name || ' ' || u.last_name AS FullName,
                                   u.email AS Email,
                                   p.avatar_key AS AvatarKey,
                                   p.phone_number AS PhoneNumber,
                                   asa.role AS CurrentRole
                               FROM users u
                               LEFT JOIN user_profiles p
                                   ON p.user_id = u.id
                               LEFT JOIN apartment_staff_assignments asa
                                   ON asa.user_id = u.id
                                   AND asa.apartment_id = @ApartmentId
                                   AND asa.revoked_on_utc IS NULL
                               WHERE u.email = @Email
                               """;

        var row = await connection.QueryFirstOrDefaultAsync<CandidateRow>(
            userSql,
            new { request.ApartmentId, Email = request.Email.Trim().ToLowerInvariant() });

        if (row is null)
        {
            return Result.Failure<StaffCandidateResponse>(UserErrors.NotFound);
        }

        return await ToCandidateResponseAsync(row, ownerId.Value, cancellationToken);
    }

    private async Task<StaffCandidateResponse> ToCandidateResponseAsync(
        CandidateRow row,
        Guid ownerId,
        CancellationToken cancellationToken)
    {
        var avatarUrl = string.IsNullOrWhiteSpace(row.AvatarKey)
            ? null
            : await fileStorageService.GeneratePresignedUrlAsync(row.AvatarKey, cancellationToken);

        return new StaffCandidateResponse
        {
            UserId = row.UserId,
            FullName = row.FullName,
            Email = row.Email,
            AvatarUrl = avatarUrl,
            PhoneNumber = row.PhoneNumber,
            IsApartmentOwner = row.UserId == ownerId,
            IsAlreadyAssigned = row.CurrentRole is not null,
            CurrentRole = row.CurrentRole
        };
    }

    internal sealed class CandidateRow
    {
        public Guid UserId { get; init; }
        public string FullName { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public string? AvatarKey { get; init; }
        public string? PhoneNumber { get; init; }
        public ApartmentStaffRole? CurrentRole { get; init; }
    }
}