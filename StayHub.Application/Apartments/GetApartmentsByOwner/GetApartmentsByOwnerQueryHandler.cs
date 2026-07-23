using Dapper;
using StayHub.Application.Abstractions.Data;
using StayHub.Application.Abstractions.Messaging;
using StayHub.Domain.Abstractions;

namespace StayHub.Application.Apartments.GetApartmentsByOwner;

internal sealed class GetApartmentsByOwnerQueryHandler(
    ISqlConnectionFactory sqlConnectionFactory)
    : IQueryHandler<GetApartmentsByOwnerQuery, IReadOnlyList<ApartmentSummaryResponse>>
{
    public async Task<Result<IReadOnlyList<ApartmentSummaryResponse>>> Handle(
        GetApartmentsByOwnerQuery request,
        CancellationToken cancellationToken)
    {
        using var connection = sqlConnectionFactory.CreateConnection();

        const string sql = """
                           SELECT
                               a.id AS Id,
                               a.name AS Name,
                               a.address_city AS City,
                               a.price_amount AS Price,
                               a.price_currency AS Currency,
                               img.url AS PrimaryImageUrl
                           FROM apartments a
                           LEFT JOIN apartment_images img
                               ON img.apartment_id = a.id AND img.is_primary = true
                           WHERE a.owner_id = @OwnerId
                           ORDER BY a.created_on_utc DESC
                           OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
                           """;

        var apartments = await connection.QueryAsync<ApartmentSummaryResponse>(
            sql,
            new
            {
                request.OwnerId,
                Offset = (request.Page - 1) * request.PageSize,
                request.PageSize
            });

        return apartments.ToList();
    }
}