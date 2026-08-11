using Dapper;
using StayHub.Application.Abstractions.Authentication;
using StayHub.Application.Abstractions.Data;
using StayHub.Application.Abstractions.Messaging;
using StayHub.Application.Bookings.GetBookingsByUser;
using StayHub.Domain.Abstractions;
using StayHub.Domain.Users;

namespace StayHub.Application.Bookings.GetBookingsByApartment;

public class GetBookingsByApartmentQueryHandler(
    ISqlConnectionFactory sqlConnectionFactory,
    IUserContext userContext)
    : IQueryHandler<GetBookingsByApartmentQuery, IReadOnlyList<BookingSummaryResponse>>
{
    public async Task<Result<IReadOnlyList<BookingSummaryResponse>>> Handle(
        GetBookingsByApartmentQuery request,
        CancellationToken cancellationToken)
    {
        using var connection = sqlConnectionFactory.CreateConnection();

        const string sql = """
                           SELECT
                               b.id AS Id,
                               b.apartment_id AS ApartmentId,
                               b.status AS Status,
                               b.total_price_amount AS TotalPriceAmount,
                               b.total_price_currency AS TotalPriceCurrency,
                               b.duration_start AS DurationStart,
                               b.duration_end AS DurationEnd
                           FROM bookings b
                           INNER JOIN apartments a ON a.id = b.apartment_id
                           WHERE b.apartment_id = @ApartmentId
                             AND (a.owner_id = @UserId OR @IsAdmin = TRUE)
                           ORDER BY b.duration_start DESC
                           OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
                           """;

        var isAdmin = userContext.Roles.Contains(Role.Admin.Name);

        var bookings = await connection.QueryAsync<BookingSummaryResponse>(
            sql,
            new
            {
                request.ApartmentId,
                userContext.UserId,
                IsAdmin = isAdmin,
                Offset = (request.Page - 1) * request.PageSize,
                request.PageSize
            });

        return bookings.ToList();
    }
}