using Dapper;
using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Abstractions.Data;
using StayHub.Application.Abstractions.Messaging;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Apartments;

namespace StayHub.Application.Apartments.GetApartmentStaff;

internal sealed class GetApartmentStaffQueryHandler(
    ISqlConnectionFactory sqlConnectionFactory,
    IUserContext userContext)
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
                               p.avatar_url AS AvatarUrl,
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

        var staff = await multi.ReadAsync<ApartmentStaffResponse>();

        return staff.ToList();
    }
}