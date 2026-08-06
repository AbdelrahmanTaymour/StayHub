using Dapper;
using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Abstractions.Data;
using StayHub.Application.Abstractions.Messaging;
using StayHub.Application.Apartments.GetApartmentsByOwner;
using StayHub.Domain.Abstractions;

namespace StayHub.Application.Favorites.GetFavoriteApartments;

internal sealed class GetFavoriteApartmentsQueryHandler(
    ISqlConnectionFactory sqlConnectionFactory,
    IUserContext userContext)
    : IQueryHandler<GetFavoriteApartmentsQuery, IReadOnlyList<ApartmentSummaryResponse>>
{
    public async Task<Result<IReadOnlyList<ApartmentSummaryResponse>>> Handle(
        GetFavoriteApartmentsQuery request,
        CancellationToken cancellationToken)
    {
        using var connection = sqlConnectionFactory.CreateConnection();

        const string sql = """
                           SELECT
                               a.id AS Id,
                               a.name AS Name,
                               a.address_city AS City,
                               a.price_amount AS PriceAmount,
                               a.price_currency AS PriceCurrency,
                               img.url AS PrimaryImageUrl
                           FROM favorite_apartments f
                           INNER JOIN apartments a ON a.id = f.apartment_id
                           LEFT JOIN apartment_images img
                               ON img.apartment_id = a.id AND img.is_primary = true
                           WHERE f.user_id = @UserId
                           ORDER BY f.created_on_utc DESC
                           OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
                           """;

        var apartments = await connection.QueryAsync<ApartmentSummaryResponse>(
            sql,
            new
            {
                userContext.UserId,
                Offset = (request.Page - 1) * request.PageSize,
                request.PageSize
            });

        return apartments.ToList();
    }
}